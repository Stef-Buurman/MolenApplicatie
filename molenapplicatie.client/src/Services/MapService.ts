import { Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import * as L from 'leaflet';
import { MolenDataClass } from '../Class/MolenDataClass';
import { MarkerInfo } from '../Interfaces/MarkerInfo';
import { MolenData } from '../Interfaces/MolenData';
import { MapInformation } from '../Class/MapInformation';

@Injectable({
  providedIn: 'root'
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
  constructor(private dialog: MatDialog,
    private router: Router,
    private route: ActivatedRoute) { }

  doesMapIdExist(mapId: string): boolean {
    return this.maps.find(map => map.MapId == mapId) != null;
  }

  updateMarker(tbn: string, molen: MolenData, mapId: string | undefined = undefined) {
    if (!mapId) mapId = this.SelectedMapId;
    var indexOfMap: number = this.maps.findIndex(map => map.MapId == mapId);
    if (indexOfMap != -1) {
      var marker = this.maps[indexOfMap].Markers.find(marker => marker.tenBruggeNumber == tbn);
      if (marker) {
        marker.marker.remove();
        this.maps[indexOfMap].Markers = this.maps[indexOfMap].Markers.filter(mark => mark.tenBruggeNumber != tbn);
        this.addMarker(molen);
      }
    }
  }

  setView(coords: L.LatLngExpression, zoom: number, mapId: string | undefined = undefined): void {
    if (!mapId) mapId = this.SelectedMapId;
    var indexOfMap: number = this.maps.findIndex(map => map.MapId == mapId);
    if (indexOfMap != -1) {
      this.maps[indexOfMap].Map.setView(coords, zoom);
    } else {
      console.error('Map is not initialized.');
    }
  }

  initMap(molens: MolenData[], mapId: string | undefined = undefined): void {
    if (!mapId) mapId = this.SelectedMapId;

    const existingMapIndex = this.maps.findIndex(map => map.MapId === mapId);
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
        attribution: '&copy; OpenStreetMap contributors'
      }).addTo(mapInstance);

      molens.forEach(molen => {
        this.addMarker(molen, mapId!);
      });
    });
  }

  addMarker(molen: MolenDataClass, mapId: string | undefined = undefined): void {
    if (!mapId) mapId = this.SelectedMapId;
    var indexOfMap: number = this.maps.findIndex(map => map.MapId == mapId);
    if (indexOfMap != -1) {
      var iconLocation = 'Assets/Icons/Molens/';
      var icon = 'windmolen_verdwenen';

      if (molen.toestand?.toLowerCase() == "restant") {
        icon = 'remainder';
      }
      else if (molen.modelType.some(m => m.name.toLowerCase() === "weidemolen")) {
        icon = 'weidemolen';
      }
      else if (molen.modelType.some(m => m.name.toLowerCase() === "paltrokmolen")) {
        icon = 'paltrokmolen';
      }
      else if (molen.modelType.some(m => m.name.toLowerCase() === "standerdmolen")) {
        icon = 'standerdmolen';
      }
      else if (molen.modelType.some(m => m.name.toLowerCase() === "wipmolen" || m.name.toLowerCase() === "spinnenkop")) {
        icon = 'wipmolen';
      }
      else if (molen.modelType.some(m => m.name.toLowerCase() === "grondzeiler")) {
        icon = 'grondzeiler';
      }
      else if (molen.modelType.some(m => m.name.toLowerCase() === "stellingmolen")) {
        icon = 'stellingmolen';
      }
      else if (molen.modelType.some(m => m.name.toLowerCase() === "beltmolen")) {
        icon = 'beltmolen';
      }

      if (molen.hasImage) {
        icon += '_has_image';
      }

      icon += '.png';

      const customIcon = L.icon({
        iconUrl: iconLocation + icon,
        iconSize: [32, 32],
        iconAnchor: [16, 32],
        popupAnchor: [0, -32]
      });

      const marker = L.marker([molen.lat, molen.long], { icon: customIcon }).addTo(this.maps[indexOfMap].Map);

      marker.on('click', () => {
        const targetUrl = `${this.router.url}/${molen.ten_Brugge_Nr}`;
        this.router.navigateByUrl(targetUrl);
      });

      this.maps[indexOfMap].Markers.push({ marker, tenBruggeNumber: molen.ten_Brugge_Nr });
    }
  }
}
