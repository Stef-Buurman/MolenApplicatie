import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ViewChild,
  ViewEncapsulation,
} from '@angular/core';
import { Router } from '@angular/router';
import * as L from 'leaflet';

import {
  MapClusterResponse,
  MapItemResponse,
  MapPointResponse,
  MolenGetMapItemsParams,
} from '../../api/generated/data-contracts';
import { ErrorService } from '../../Services/ErrorService';
import { SharedDataService } from '../../Services/SharedDataService';
import { Toasts } from '../../Utils/Toasts';
import { GetMolenIcon } from '../../Utils/GetMolenIcon';
import { molenGetMapItems } from '../../api/methods/Molen.api';

interface MolenIconProperties {
  toestand?: string | null;
  types?: string[] | null;
  hasImage?: boolean | null;
}

type MolenMapPointResponse = MapPointResponse & MolenIconProperties;
type MolenMapClusterResponse = MapClusterResponse & MolenIconProperties;

@Component({
  selector: 'app-molen-clustered-map',
  standalone: false,
  templateUrl: './molen-clustered-map.component.html',
  styleUrl: './molen-clustered-map.component.scss',
  encapsulation: ViewEncapsulation.None,
})
export class MolenClusteredMapComponent
  implements AfterViewInit, OnChanges, OnDestroy
{
  @ViewChild('mapContainer', { static: true })
  private mapContainer!: ElementRef<HTMLDivElement>;

  @Input() title: string = 'Molens';
  @Input() showTitle: boolean = true;

  @Input() initialLatitude: number = 52;
  @Input() initialLongitude: number = 4.4;
  @Input() initialZoom: number = 10;

  @Input() useCurrentLocation: boolean = false;
  @Input() currentLocationZoom: number = 12;

  @Input() minimumZoom: number = 3;
  @Input() maximumZoom: number = 19;
  @Input() showClusterOutlines: boolean = true;

  private map?: L.Map;
  private mapItemsLayer?: L.LayerGroup;
  private outlineLayer?: L.Polygon;

  private resizeObserver?: ResizeObserver;
  private reloadTimeout?: ReturnType<typeof setTimeout>;
  private abortController?: AbortController;

  private requestSequence: number = 0;
  private currentRequestKey?: string;
  private lastLoadedRequestKey?: string;

  private mapHasUsableSize: boolean = false;
  private hasLoadedOnce: boolean = false;
  private hasShownLoadedToast: boolean = false;

  constructor(
    private router: Router,
    private sharedData: SharedDataService,
    private errors: ErrorService,
    private toasts: Toasts,
  ) {}

  ngAfterViewInit(): void {
    this.initializeMap();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (
      changes['useCurrentLocation'] &&
      !changes['useCurrentLocation'].firstChange &&
      changes['useCurrentLocation'].currentValue === true &&
      this.map
    ) {
      this.goToCurrentLocation();
    }
  }

  ngOnDestroy(): void {
    if (this.reloadTimeout) {
      clearTimeout(this.reloadTimeout);
    }

    this.abortController?.abort();
    this.resizeObserver?.disconnect();

    this.map?.off();
    this.map?.remove();

    this.map = undefined;
    this.mapItemsLayer = undefined;
    this.outlineLayer = undefined;
  }

  public setView(
    latitude: number,
    longitude: number,
    zoom: number = this.currentLocationZoom,
  ): void {
    this.map?.setView([latitude, longitude], zoom);
  }

  public reload(): void {
    this.lastLoadedRequestKey = undefined;
    this.refreshMapSizeAndLoad();
  }

  private initializeMap(): void {
    if (this.map) return;

    this.map = L.map(this.mapContainer.nativeElement, {
      minZoom: this.minimumZoom,
      maxZoom: this.maximumZoom,
      zoomControl: true,
      preferCanvas: true,
    });

    this.mapItemsLayer = L.layerGroup().addTo(this.map);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      minZoom: this.minimumZoom,
      maxZoom: this.maximumZoom,
      attribution: '',
    }).addTo(this.map);

    this.map.on('moveend', () => {
      this.scheduleReload();
    });

    this.map.on('popupclose', () => {
      this.removeOutline();
    });

    this.map.setView(
      [this.initialLatitude, this.initialLongitude],
      this.initialZoom,
    );

    this.observeMapSize();

    this.map.whenReady(() => {
      this.refreshMapSizeAndLoad();
    });

    if (this.useCurrentLocation) {
      this.goToCurrentLocation();
    }
  }

  private observeMapSize(): void {
    if (!this.map) return;

    const container = this.mapContainer.nativeElement;

    if (typeof ResizeObserver === 'undefined') {
      this.refreshMapSizeAndLoad();
      return;
    }

    this.resizeObserver = new ResizeObserver((entries) => {
      const entry = entries[0];

      if (!entry) return;

      const width = entry.contentRect.width;
      const height = entry.contentRect.height;

      this.mapHasUsableSize = width > 1 && height > 1;

      if (!this.mapHasUsableSize) return;

      requestAnimationFrame(() => {
        if (!this.map) return;

        this.map.invalidateSize({
          animate: false,
          pan: false,
        });

        this.scheduleReload(0);
      });
    });

    this.resizeObserver.observe(container);
  }

  private refreshMapSizeAndLoad(): void {
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        if (!this.map) return;

        const container = this.mapContainer.nativeElement;

        this.mapHasUsableSize =
          container.clientWidth > 1 && container.clientHeight > 1;

        if (!this.mapHasUsableSize) return;

        this.map.invalidateSize({
          animate: false,
          pan: false,
        });

        this.scheduleReload(0);
      });
    });
  }

  private goToCurrentLocation(): void {
    if (!this.map) return;
    if (typeof navigator === 'undefined') return;
    if (!navigator.geolocation) return;

    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.map?.setView(
          [position.coords.latitude, position.coords.longitude],
          this.currentLocationZoom,
        );
      },
      (error) => {
        console.warn('De huidige locatie kon niet worden opgehaald.', error);
      },
      {
        enableHighAccuracy: false,
        timeout: 8000,
        maximumAge: 300000,
      },
    );
  }

  private scheduleReload(delay: number = 175): void {
    if (!this.mapHasUsableSize) return;

    if (this.reloadTimeout) {
      clearTimeout(this.reloadTimeout);
    }

    this.reloadTimeout = setTimeout(() => {
      void this.loadVisibleMapItems();
    }, delay);
  }

  private async loadVisibleMapItems(): Promise<void> {
    if (!this.map || !this.mapHasUsableSize) return;

    const container = this.mapContainer.nativeElement;

    if (container.clientWidth <= 1 || container.clientHeight <= 1) {
      this.mapHasUsableSize = false;
      return;
    }

    const bounds = this.map.getBounds();

    if (!bounds.isValid()) return;

    const west = bounds.getWest();
    const south = bounds.getSouth();
    const east = bounds.getEast();
    const north = bounds.getNorth();
    const zoom = Math.round(this.map.getZoom());

    const longitudeDifference = Math.abs(east - west);
    const latitudeDifference = Math.abs(north - south);

    if (longitudeDifference < 0.000001 || latitudeDifference < 0.000001) {
      console.warn(
        'Map request overgeslagen omdat de viewport nog geen bruikbare afmetingen heeft.',
        {
          containerWidth: container.clientWidth,
          containerHeight: container.clientHeight,
          west,
          south,
          east,
          north,
          zoom,
        },
      );

      return;
    }

    const query: MolenGetMapItemsParams = {
      west,
      south,
      east,
      north,
      zoom,
    };

    const requestKey = this.createRequestKey(query);

    if (
      requestKey === this.lastLoadedRequestKey ||
      requestKey === this.currentRequestKey
    ) {
      return;
    }

    this.abortController?.abort();

    const abortController = new AbortController();
    const requestSequence = ++this.requestSequence;
    const showGlobalLoader = !this.hasLoadedOnce;

    this.abortController = abortController;
    this.currentRequestKey = requestKey;

    if (showGlobalLoader) {
      this.sharedData.IsLoadingTrue();
    }

    try {
      const result = await molenGetMapItems(query, {
        params: {
          signal: abortController.signal,
        },
      });

      if (
        abortController.signal.aborted ||
        requestSequence !== this.requestSequence
      ) {
        return;
      }

      if (!result.ok) {
        throw (
          result.error ??
          new Error('De server heeft geen geldige kaartitems teruggegeven.')
        );
      }

      this.renderMapItems(result.response);

      this.lastLoadedRequestKey = requestKey;
      this.hasLoadedOnce = true;

      if (!this.hasShownLoadedToast) {
        this.hasShownLoadedToast = true;
        this.toasts.showSuccess('De kaart is geladen!');
      }
    } catch (error) {
      if (
        abortController.signal.aborted ||
        requestSequence !== this.requestSequence
      ) {
        return;
      }

      console.error('De kaartitems konden niet worden geladen.', error);

      this.errors.AddError(this.getErrorMessage(error));
      this.toasts.showError('De kaartitems konden niet geladen worden!');
    } finally {
      if (requestSequence === this.requestSequence) {
        this.currentRequestKey = undefined;
        this.abortController = undefined;

        if (showGlobalLoader) {
          this.sharedData.IsLoadingFalse();
        }
      }
    }
  }

  private renderMapItems(mapItems: MapItemResponse[]): void {
    if (!this.map || !this.mapItemsLayer) return;

    this.removeOutline();
    this.mapItemsLayer.clearLayers();

    for (const mapItem of mapItems) {
      if (this.isCluster(mapItem)) {
        this.addCluster(mapItem);
      } else {
        this.addPoint(mapItem);
      }
    }
  }

  private addPoint(point: MapPointResponse): void {
    if (!this.mapItemsLayer) return;

    const molenPoint = point as MolenMapPointResponse;

    const marker = L.marker([point.latitude, point.longitude], {
      icon: this.createMolenIcon(molenPoint),
      bubblingMouseEvents: false,
      keyboard: true,
      title: point.popupText ?? 'Molen',
    });

    marker.on('click', () => {
      if (!this.map) return;

      if (!point.popupText) {
        this.navigateToUrl(point.url);
        return;
      }

      L.popup({
        closeButton: true,
        autoPan: true,
        maxWidth: 360,
        className: 'molen-map-popup',
      })
        .setLatLng([point.latitude, point.longitude])
        .setContent(this.createPointPopup(point))
        .openOn(this.map);
    });

    marker.addTo(this.mapItemsLayer);
  }

  private addCluster(cluster: MapClusterResponse): void {
    if (!this.mapItemsLayer) return;

    const molenCluster = cluster as MolenMapClusterResponse;
    const isSingleMolen = cluster.pointCount === 1;

    const marker = L.marker([cluster.latitude, cluster.longitude], {
      icon: isSingleMolen
        ? this.createMolenIcon(molenCluster)
        : this.createClusterIcon(cluster.pointCount),
      bubblingMouseEvents: false,
      keyboard: true,
      title: isSingleMolen
        ? '1 molen'
        : `${cluster.pointCount.toLocaleString('nl-NL')} molens`,
    });

    marker.on('mouseover', () => {
      if (!isSingleMolen) {
        this.showOutline(cluster);
      }
    });

    marker.on('mouseout', () => {
      if (!isSingleMolen) {
        this.removeOutline();
      }
    });

    marker.on('click', () => {
      if (isSingleMolen) {
        this.handleSingleMolenClusterClick(cluster);
        return;
      }

      this.handleClusterClick(cluster);
    });

    marker.addTo(this.mapItemsLayer);
  }

  private handleSingleMolenClusterClick(cluster: MapClusterResponse): void {
    if (!this.map) return;

    const point = cluster.popupData?.pointData?.[0];

    if (!point) {
      this.handleClusterClick(cluster);
      return;
    }

    if (!point.popupText) {
      this.navigateToUrl(point.url);
      return;
    }

    L.popup({
      closeButton: true,
      autoPan: true,
      maxWidth: 360,
      className: 'molen-map-popup',
    })
      .setLatLng([cluster.latitude, cluster.longitude])
      .setContent(this.createSingleClusterPopup(point.url, point.popupText))
      .openOn(this.map);
  }

  private handleClusterClick(cluster: MapClusterResponse): void {
    if (!this.map) return;

    const currentZoom = this.map.getZoom();
    const expansionZoom = Math.min(
      cluster.expansionZoom,
      this.map.getMaxZoom(),
    );

    if (currentZoom < expansionZoom) {
      this.removeOutline();

      this.map.setView([cluster.latitude, cluster.longitude], expansionZoom, {
        animate: true,
      });

      return;
    }

    if (
      cluster.popupData?.pointData &&
      cluster.popupData.pointData.length > 0
    ) {
      L.popup({
        closeButton: true,
        autoPan: true,
        maxWidth: 400,
        className: 'molen-map-popup',
      })
        .setLatLng([cluster.latitude, cluster.longitude])
        .setContent(this.createClusterPopup(cluster))
        .openOn(this.map);

      return;
    }

    this.map.setView(
      [cluster.latitude, cluster.longitude],
      Math.min(currentZoom + 1, this.map.getMaxZoom()),
      {
        animate: true,
      },
    );
  }

  private createMolenIcon(item: MolenIconProperties): L.Icon {
    const iconFile = GetMolenIcon(
      item.toestand ?? undefined,
      item.types ?? undefined,
      item.hasImage ?? false,
    );

    return L.icon({
      iconUrl: `Assets/Icons/Molens/${iconFile}`,
      iconSize: [32, 32],
      iconAnchor: [16, 32],
      popupAnchor: [0, -32],
    });
  }

  private createClusterIcon(pointCount: number): L.DivIcon {
    const formattedPointCount = pointCount.toLocaleString('nl-NL');

    return L.divIcon({
      className: 'molen-map-cluster-wrapper',
      html: `
        <div class="molen-map-cluster">
          <span>${formattedPointCount}</span>
        </div>
      `,
      iconSize: [48, 48],
      iconAnchor: [24, 24],
    });
  }

  private createPointPopup(point: MapPointResponse): HTMLElement {
    const container = document.createElement('div');

    container.className = 'molen-map-popup-content';

    if (point.popupText) {
      const text = document.createElement('div');

      text.className = 'molen-map-popup-text';
      text.textContent = point.popupText;

      container.appendChild(text);
    }

    container.appendChild(
      this.createNavigationButton(point.url, 'Bekijk details'),
    );

    return container;
  }

  private createSingleClusterPopup(
    url: string,
    popupText: string,
  ): HTMLElement {
    const container = document.createElement('div');

    container.className = 'molen-map-popup-content';

    const text = document.createElement('div');

    text.className = 'molen-map-popup-text';
    text.textContent = popupText;

    container.appendChild(text);
    container.appendChild(this.createNavigationButton(url, 'Bekijk details'));

    return container;
  }

  private createClusterPopup(cluster: MapClusterResponse): HTMLElement {
    const container = document.createElement('div');

    container.className = 'molen-map-popup-content';

    const title = document.createElement('strong');

    title.className = 'molen-map-popup-title';
    title.textContent =
      cluster.popupData?.title ??
      `${cluster.pointCount.toLocaleString('nl-NL')} molens`;

    container.appendChild(title);

    const itemsContainer = document.createElement('div');

    itemsContainer.className = 'molen-map-popup-items';

    for (const point of cluster.popupData?.pointData ?? []) {
      itemsContainer.appendChild(
        this.createNavigationButton(
          point.url,
          point.popupText || 'Bekijk molen',
        ),
      );
    }

    container.appendChild(itemsContainer);

    return container;
  }

  private createNavigationButton(url: string, text: string): HTMLButtonElement {
    const button = document.createElement('button');

    button.type = 'button';
    button.className = 'molen-map-popup-button';
    button.textContent = text;

    button.addEventListener('click', (event) => {
      event.preventDefault();
      event.stopPropagation();

      this.map?.closePopup();
      this.navigateToUrl(url);
    });

    return button;
  }

  private navigateToUrl(url: string): void {
    if (!url) return;

    if (/^https?:\/\//i.test(url)) {
      window.location.assign(url);
      return;
    }

    void this.router.navigateByUrl(url);
  }

  private showOutline(cluster: MapClusterResponse): void {
    if (!this.map || !this.showClusterOutlines) return;
    if (!cluster.outline || cluster.outline.length < 3) return;

    this.removeOutline();

    const coordinates: L.LatLngExpression[] = cluster.outline.map(
      (coordinate) => [coordinate.latitude, coordinate.longitude],
    );

    this.outlineLayer = L.polygon(coordinates, {
      color: '#0058a2',
      weight: 2,
      opacity: 0.9,
      fillColor: '#0058a2',
      fillOpacity: 0.08,
      interactive: false,
    }).addTo(this.map);
  }

  private removeOutline(): void {
    if (!this.outlineLayer) return;

    this.outlineLayer.remove();
    this.outlineLayer = undefined;
  }

  private createRequestKey(query: MolenGetMapItemsParams): string {
    return [
      query.west?.toFixed(5),
      query.south?.toFixed(5),
      query.east?.toFixed(5),
      query.north?.toFixed(5),
      query.zoom,
    ].join('|');
  }

  private isCluster(mapItem: MapItemResponse): mapItem is MapClusterResponse {
    return mapItem.type === 'cluster';
  }

  private getErrorMessage(error: unknown): string {
    if (error instanceof Error) {
      return error.message;
    }

    if (typeof error === 'string') {
      return error;
    }

    if (error && typeof error === 'object') {
      if ('detail' in error && typeof error.detail === 'string') {
        return error.detail;
      }

      if ('title' in error && typeof error.title === 'string') {
        return error.title;
      }
    }

    return 'De kaartitems konden niet geladen worden.';
  }
}
