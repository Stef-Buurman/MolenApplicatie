import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import * as L from 'leaflet';
import { MapData } from '../Interfaces/Map/MapData';
import { MapInformation } from '../Class/MapInformation';
import { GetMolenIcon } from '../Utils/GetMolenIcon';

@Injectable({
  providedIn: 'root',
})
export class MapService {
  private maps: MapInformation[] = [];
  private _selectedMapId!: string;
  get SelectedMapId(): string {
    return this._selectedMapId;
  }
  set SelectedMapId(mapId: string) {
    this._selectedMapId = mapId;
  }
  constructor(private router: Router) {}

  doesMapIdExist(mapId: string): boolean {
    return this.maps.find((map) => map.MapId == mapId) != null;
  }

  doesTenBruggeNumberExist(
    tbn: string,
    mapId: string | undefined = undefined
  ): boolean {
    if (!mapId) mapId = this.SelectedMapId;
    var indexOfMap: number = this.maps.findIndex((map) => map.MapId == mapId);
    if (indexOfMap != -1) {
      return (
        this.maps[indexOfMap].Markers.find(
          (marker) => marker.tenBruggeNumber == tbn
        ) != null
      );
    }
    return false;
  }

  updateMarker(
    tbn: string,
    molen: MapData,
    mapId: string | undefined = undefined
  ) {
    if (!mapId) mapId = this.SelectedMapId;
    var indexOfMap: number = this.maps.findIndex((map) => map.MapId == mapId);
    if (indexOfMap != -1) {
      var marker = this.maps[indexOfMap].Markers.find(
        (marker) => marker.tenBruggeNumber == tbn
      );
      if (marker) {
        marker.marker.remove();
        this.maps[indexOfMap].Markers = this.maps[indexOfMap].Markers.filter(
          (mark) => mark.tenBruggeNumber != tbn
        );
        this.addMarker(molen);
      }
    }
  }

  setView(
    coords: L.LatLngExpression,
    zoom: number,
    mapId: string | undefined = undefined
  ): void {
    if (!mapId) mapId = this.SelectedMapId;
    var indexOfMap: number = this.maps.findIndex((map) => map.MapId == mapId);
    if (indexOfMap != -1) {
      this.maps[indexOfMap].Map.setView(coords, zoom);
    } else {
      console.error('Map is not initialized.');
    }
  }

  initMap(molens: MapData[], mapId: string | undefined = undefined): void {
    if (!mapId) mapId = this.SelectedMapId;

    const existingMapIndex = this.maps.findIndex((map) => map.MapId === mapId);
    if (existingMapIndex !== -1) {
      this.maps[existingMapIndex].Map.remove();
      this.maps.splice(existingMapIndex, 1);
    }
    setTimeout(() => {
      const newMap = new MapInformation(mapId, L.map(mapId!));
      this.maps.push(newMap);
      const mapInstance = newMap.Map;

      mapInstance.setView([52, 4.4], 10);

      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '',
      }).addTo(mapInstance);

      molens.forEach((molen) => {
        this.addMarker(molen, mapId!);
      });
    });
  }

  addMarker(molen: MapData, mapId: string | undefined = undefined): void {
    if (!mapId) mapId = this.SelectedMapId;
    var indexOfMap: number = this.maps.findIndex((map) => map.MapId == mapId);
    if (indexOfMap != -1) {
      var iconLocation = 'Assets/Icons/Molens/';
      var icon = GetMolenIcon(
        molen.toestand,
        molen.types,
        molen.hasImage
      );

      const customIcon = L.icon({
        iconUrl: iconLocation + icon,
        iconSize: [32, 32],
        iconAnchor: [16, 32],
        popupAnchor: [0, -32],
      });

      const marker = L.marker([molen.latitude, molen.longitude], {
        icon: customIcon,
      }).addTo(this.maps[indexOfMap].Map);

      marker.on('click', () => {
        const targetUrl = `${this.router.url}/${molen.reference}`;
        this.router.navigateByUrl(targetUrl);
      });

      this.maps[indexOfMap].Markers.push({
        marker,
        tenBruggeNumber: molen.reference,
      });
    }
  }
}
