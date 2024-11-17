import { Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import * as L from 'leaflet';
import { MolenDataClass } from '../Class/MolenDataClass';
import { MarkerInfo } from '../Interfaces/MarkerInfo';
import { MolenData } from '../Interfaces/MolenData';

@Injectable({
  providedIn: 'root'
})
export class MapService {
  private map: L.Map | null = null;
  private _markers: MarkerInfo[] = [];
  public get markers(): MarkerInfo[] {
    return this._markers;
  }
  constructor(private dialog: MatDialog,
    private router: Router) { }


  getMarker(tbn:string): MarkerInfo | undefined {
    return this.markers.find(marker => marker.tenBruggeNumber == tbn);
  }

  updateMarker(tbn: string, molen:MolenData) {
    var marker = this.getMarker(tbn);
    if (marker) {
      marker.marker.remove();
      this._markers = this.markers.filter(mark => mark.tenBruggeNumber != tbn);
      this.addMarker(molen);
    }
  }

  setView(coords: L.LatLngExpression, zoom: number): void {
    if (this.map) {
      this.map.setView(coords, zoom);
    } else {
      console.error('Map is not initialized.');
    }
  }

  initMap(molens: MolenData[]): void {
    setTimeout(() => {
      this.map = L.map('map');
      this.map.setView([52, 4.4], 10);

      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
      }).addTo(this.map);

      this.saveMap(this.map)

      molens.forEach(molen => {
        this.addMarker(molen);
      });
    });
  }

  saveMap(map: L.Map) {
    if (map) {
      this.map = map;
    }
  }

  addMarker(molen: MolenDataClass): void {
    if (this.map) {
      var iconLocation = 'Assets/Icons/Molens/';
      var icon = 'windmolen_verdwenen';

      if (molen.modelType.some(m => m.name.toLowerCase() === "weidemolen")) {
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

      const marker = L.marker([molen.north, molen.east], { icon: customIcon }).addTo(this.map);

      marker.on('click', () => {
        this.router.navigate(['/' + molen.ten_Brugge_Nr]);
      });

      this.markers.push({ marker, tenBruggeNumber: molen.ten_Brugge_Nr });
    }
  }
}
