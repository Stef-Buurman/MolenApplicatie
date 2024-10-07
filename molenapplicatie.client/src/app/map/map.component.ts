import { Component, AfterViewInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as L from 'leaflet';
import { MolenDataClass } from '../../Class/MolenDataClass';
import { MatDialog } from '@angular/material/dialog';
import { MolenDialogComponent } from '../molen-dialog/molen-dialog.component';

@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrl: './map.component.css'
})
export class MapComponent {
  molens: MolenDataClass[] = [];

  constructor(private http: HttpClient,
    private dialog: MatDialog) { }

  getMolens(): void {
    this.http.get<MolenDataClass[]>('/api/all_molen_locations').subscribe(
      (result) => {
        this.molens = result;
        this.initMap();
      },
      (error) => {
        console.error(error);
      }
    );
  }

  ngAfterViewInit(): void {
    this.getMolens();
  }

  private initMap(): void {
    const map = L.map('map').setView([52, 4.4], 10);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);
    const customIcon = L.icon({
      iconUrl: 'Assets/Icons/grondzeiler.png',
      iconSize: [32, 32],
      iconAnchor: [16, 32],
      popupAnchor: [0, -32]
    });

    this.molens.forEach(molen => {
      const marker = L.marker([molen.north, molen.east], { icon: customIcon })
        .addTo(map);
      marker.on('click', () => {
        this.onMarkerClick(molen.ten_Brugge_Nr);
      });
    });
  }

  private onMarkerClick(tenBruggeNr: any): void {
    console.log('Opening dialog with tenBruggeNr:', tenBruggeNr);
    const dialogRef = this.dialog.open(MolenDialogComponent, {
      width: '250px',
      data: { tenBruggeNr }
    });
    console.log('Dialog opened:', dialogRef);
  }

}
