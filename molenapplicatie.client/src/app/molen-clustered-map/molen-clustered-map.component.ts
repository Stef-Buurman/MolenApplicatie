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
import { Subscription } from 'rxjs';
import * as L from 'leaflet';

import {
  MapClusterResponse,
  MapItemResponse,
  MapPointResponse,
  MolenGetMapItemsParams,
} from '../../api/generated/data-contracts';
import { SharedDataService } from '../../Services/SharedDataService';
import { MolenService } from '../../Services/MolenService';
import { FilterFormValues } from '../../Interfaces/Filters/Filter';
import { Toasts } from '../../Utils/Toasts';
import { CacheManager } from '../../Utils/CacheManager';
import { GetMolenIcon } from '../../Utils/GetMolenIcon';
import { MolenCacheKeys } from '../../Utils/MolenCacheKeys';
import { molenGetMapItems } from '../../api/methods/Molen.api';

interface RenderedMapMarker {
  marker: L.Marker;
  signature: string;
}

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
  @Input() filters: FilterFormValues[] = [];

  private map?: L.Map;
  private mapItemsLayer?: L.LayerGroup;
  private outlineLayer?: L.Polygon;
  private renderedMarkers = new Map<string, RenderedMapMarker>();

  private resizeObserver?: ResizeObserver;
  private reloadTimeout?: ReturnType<typeof setTimeout>;
  private abortController?: AbortController;
  private mapRefreshSubscription?: Subscription;

  private requestSequence: number = 0;
  private currentRequestKey?: string;
  private lastLoadedRequestKey?: string;

  private mapHasUsableSize: boolean = false;
  private hasLoadedOnce: boolean = false;
  private hasShownLoadedToast: boolean = false;
  private readonly initialRequestTimeoutMs: number = 30000;
  private readonly maximumWebMercatorLatitude: number = 85.05112878;

  constructor(
    private router: Router,
    private sharedData: SharedDataService,
    private molenService: MolenService,
    private toasts: Toasts,
  ) {}

  ngAfterViewInit(): void {
    this.mapRefreshSubscription = this.molenService.mapRefresh$.subscribe(
      () => {
        this.reload();
      },
    );
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

    if (changes['filters'] && !changes['filters'].firstChange && this.map) {
      this.reload();
    }
  }

  ngOnDestroy(): void {
    const initialLoadWasPending =
      !this.hasLoadedOnce && !!this.currentRequestKey;

    if (this.reloadTimeout) {
      clearTimeout(this.reloadTimeout);
    }

    this.abortController?.abort();
    this.mapRefreshSubscription?.unsubscribe();
    this.resizeObserver?.disconnect();

    this.map?.off();
    this.map?.remove();

    this.map = undefined;
    this.mapItemsLayer = undefined;
    this.outlineLayer = undefined;
    this.renderedMarkers.clear();

    if (initialLoadWasPending) {
      this.sharedData.IsLoadingFalse();
    }
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
    let east = bounds.getEast();

    // Keep a continuous longitude interval. Leaflet deliberately returns values
    // below -180 or above 180 for repeated world copies and the backend supports it.
    while (east <= west) {
      east += 360;
    }

    // Large map viewports can extend beyond the valid Web Mercator latitude.
    // Clamp before sending the request so ASP.NET never returns a validation error.
    const south = this.clampLatitude(bounds.getSouth());
    const north = this.clampLatitude(bounds.getNorth());
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
      molenType: this.getStringFilterValue('MolenType'),
      land: this.getStringFilterValue('Land'),
      provincie: this.getStringFilterValue('Provincie'),
      molenState: this.getStringFilterValue('MolenState'),
      hasImage: this.getBooleanFilterValue('HasImage'),
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
      const mapItems = await CacheManager.getOrSet(
        MolenCacheKeys.mapItems(requestKey),
        async () => {
          const result = await molenGetMapItems(query, {
            params: {
              signal: abortController.signal,
              timeoutMs: this.initialRequestTimeoutMs,
            },
          });

          if (!result.ok) {
            throw (
              result.error ??
              new Error('De server heeft geen geldige kaartitems teruggegeven.')
            );
          }

          return result.response;
        },
        2,
      );

      if (
        abortController.signal.aborted ||
        requestSequence !== this.requestSequence
      ) {
        return;
      }

      this.renderMapItems(mapItems);

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

      this.toasts.showError(this.getErrorMessage(error));
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

    const nextKeys = new Set<string>();

    for (const mapItem of mapItems) {
      const key = this.getMapItemKey(mapItem);
      const signature = this.getMapItemSignature(mapItem);
      nextKeys.add(key);

      const renderedMarker = this.renderedMarkers.get(key);

      if (renderedMarker?.signature === signature) {
        continue;
      }

      if (renderedMarker) {
        this.mapItemsLayer.removeLayer(renderedMarker.marker);
        this.renderedMarkers.delete(key);
      }

      const marker = this.isCluster(mapItem)
        ? this.createClusterMarker(mapItem)
        : this.createPointMarker(mapItem);

      marker.addTo(this.mapItemsLayer);
      this.renderedMarkers.set(key, { marker, signature });
    }

    for (const [key, renderedMarker] of this.renderedMarkers) {
      if (nextKeys.has(key)) continue;

      this.mapItemsLayer.removeLayer(renderedMarker.marker);
      this.renderedMarkers.delete(key);
    }
  }

  private createPointMarker(point: MapPointResponse): L.Marker {
    const molenPoint = point as MolenMapPointResponse;

    const marker = L.marker([point.latitude, point.longitude], {
      icon: this.createMolenIcon(molenPoint),
      bubblingMouseEvents: false,
      keyboard: true,
      title: point.popupText ?? 'Molen',
    });

    marker.on('click', () => {
      this.navigateToUrl(point.url);
    });

    return marker;
  }

  private createClusterMarker(cluster: MapClusterResponse): L.Marker {
    const molenCluster = cluster as MolenMapClusterResponse;
    const isSingleMolen = cluster.pointCount === 1;
    const isExactLocationCluster = this.isExactLocationCluster(cluster);

    const marker = L.marker([cluster.latitude, cluster.longitude], {
      icon: isSingleMolen
        ? this.createMolenIcon(molenCluster)
        : this.createClusterIcon(cluster.pointCount, isExactLocationCluster),
      bubblingMouseEvents: false,
      keyboard: true,
      title: isSingleMolen
        ? '1 molen'
        : isExactLocationCluster
          ? `${cluster.pointCount.toLocaleString('nl-NL')} molens op exact dezelfde locatie`
          : `${cluster.pointCount.toLocaleString('nl-NL')} molens`,
    });

    marker.on('mouseover', () => {
      if (!isSingleMolen && !isExactLocationCluster) {
        this.showOutline(cluster);
      }
    });

    marker.on('mouseout', () => {
      if (!isSingleMolen && !isExactLocationCluster) {
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

    return marker;
  }

  private getMapItemKey(mapItem: MapItemResponse): string {
    return this.isCluster(mapItem)
      ? `cluster:${mapItem.clusterId}`
      : `point:${mapItem.url}`;
  }

  private getMapItemSignature(mapItem: MapItemResponse): string {
    if (this.isCluster(mapItem)) {
      return JSON.stringify([
        mapItem.latitude,
        mapItem.longitude,
        mapItem.pointCount,
        mapItem.expansionZoom,
        mapItem.popupData,
        mapItem.outline,
      ]);
    }

    const molenPoint = mapItem as MolenMapPointResponse;

    return JSON.stringify([
      mapItem.latitude,
      mapItem.longitude,
      mapItem.url,
      mapItem.popupText,
      molenPoint.toestand,
      molenPoint.types,
      molenPoint.hasImage,
    ]);
  }

  private handleSingleMolenClusterClick(cluster: MapClusterResponse): void {
    const point = cluster.popupData?.pointData?.[0];

    if (point?.url) {
      this.navigateToUrl(point.url);
      return;
    }

    this.handleClusterClick(cluster);
  }

  private handleClusterClick(cluster: MapClusterResponse): void {
    if (!this.map) return;

    if (
      cluster.popupData?.pointData &&
      cluster.popupData.pointData.length > 0
    ) {
      this.removeOutline();

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

  private createClusterIcon(
    pointCount: number,
    isExactLocationCluster: boolean,
  ): L.DivIcon {
    const formattedPointCount = pointCount.toLocaleString('nl-NL');
    const size = this.getClusterIconSize(pointCount);
    const densityClass = this.getClusterDensityClass(pointCount);
    const exactLocationClass = isExactLocationCluster
      ? ' molen-map-cluster--exact-location'
      : '';
    const exactLocationIndicator = isExactLocationCluster
      ? `
          <span class="molen-map-cluster-stack" aria-hidden="true">
            <span></span>
            <span></span>
          </span>
        `
      : '';
    const anchor = size / 2;

    return L.divIcon({
      className: 'molen-map-cluster-wrapper',
      html: `
        <div
          class="molen-map-cluster molen-map-cluster--${densityClass}${exactLocationClass}"
          style="--molen-cluster-size: ${size}px"
        >
          <span class="molen-map-cluster-count">${formattedPointCount}</span>
          ${exactLocationIndicator}
        </div>
      `,
      iconSize: [size, size],
      iconAnchor: [anchor, anchor],
    });
  }

  private getClusterIconSize(pointCount: number): number {
    const minimumSize = 36;
    const maximumSize = 60;
    const maximumCountForSizing = 1000;
    const normalizedCount = Math.min(
      Math.max(pointCount, 2),
      maximumCountForSizing,
    );
    const progress =
      Math.log10(normalizedCount) / Math.log10(maximumCountForSizing);

    return Math.round(minimumSize + progress * (maximumSize - minimumSize));
  }

  private getClusterDensityClass(pointCount: number): string {
    if (pointCount >= 250) return 'very-high';
    if (pointCount >= 100) return 'high';
    if (pointCount >= 50) return 'medium-high';
    if (pointCount >= 25) return 'medium';
    if (pointCount >= 10) return 'low-medium';

    return 'low';
  }

  private isExactLocationCluster(cluster: MapClusterResponse): boolean {
    return (cluster.popupData?.pointData?.length ?? 0) > 1;
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

  private clampLatitude(latitude: number): number {
    return Math.max(
      -this.maximumWebMercatorLatitude,
      Math.min(this.maximumWebMercatorLatitude, latitude),
    );
  }

  private createRequestKey(query: MolenGetMapItemsParams): string {
    return [
      query.west?.toFixed(5),
      query.south?.toFixed(5),
      query.east?.toFixed(5),
      query.north?.toFixed(5),
      query.zoom,
      query.molenType ?? '',
      query.provincie ?? '',
      query.molenState ?? '',
      query.hasImage?.toString() ?? '',
    ].join('|');
  }

  private getStringFilterValue(filterName: string): string | undefined {
    const value = this.filters.find(
      (filter) => filter.filterName === filterName,
    )?.value;

    return typeof value === 'string' && value.trim() ? value.trim() : undefined;
  }

  private getBooleanFilterValue(filterName: string): boolean | undefined {
    const value = this.filters.find(
      (filter) => filter.filterName === filterName,
    )?.value;

    return typeof value === 'boolean' ? value : undefined;
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
      if (
        'errors' in error &&
        error.errors &&
        typeof error.errors === 'object'
      ) {
        const validationMessages = Object.values(error.errors)
          .flatMap((messages) =>
            Array.isArray(messages)
              ? messages.filter(
                  (message): message is string => typeof message === 'string',
                )
              : [],
          )
          .filter((message) => message.trim().length > 0);

        if (validationMessages.length > 0) {
          return validationMessages.join(' ');
        }
      }

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
