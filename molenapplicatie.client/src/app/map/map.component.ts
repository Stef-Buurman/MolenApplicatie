import { Component, AfterViewInit, input, Input, EventEmitter, Output } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as L from 'leaflet';
import { MolenDataClass } from '../../Class/MolenDataClass';
import { MatDialog } from '@angular/material/dialog';
import { MolenDialogComponent } from '../molen-dialog/molen-dialog.component';
import { Toasts } from '../../Utils/Toasts';
import { InitializeDataStatus } from '../../Enums/InitializeDataStatus';

@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrl: './map.component.scss'
})
export class MapComponent {
  @Input() map: any;
  @Output() mapChange = new EventEmitter<any>();
  @Input() error: boolean = false;
  @Output() errorChange = new EventEmitter<boolean>();
  @Input() status!: InitializeDataStatus;
  @Output() statusChange = new EventEmitter<InitializeDataStatus>();
  molens: MolenDataClass[] = [];
  molenDataError: boolean = false;

  constructor(private http: HttpClient,
    private toasts: Toasts,
    private dialog: MatDialog) { }

  getMolens(): void {
    this.http.get<MolenDataClass[]>('/api/all_molen_locations').subscribe({
      next: (result) => {
        this.molens = result;
        this.initMap();
      },
      error: (error) => {
        this.errorChange.emit(true);
        this.statusChange.emit(InitializeDataStatus.Error);
        this.toasts.showError("De molens kunnen niet geladen worden!");
      },
      complete: () => {
        this.errorChange.emit(false);
        this.toasts.showSuccess("Molens zijn geladen!");
        this.statusChange.emit(InitializeDataStatus.Success);
      }
    });
  }

  test() {
    this.map.setView([5, 4.4], 10);
  }

  ngAfterViewInit(): void {
    this.getMolens();
  }

  private initMap(): void {
    this.map = L.map('map');
    this.map.setView([52, 4.4], 10);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(this.map);

    const customIcon = L.icon({
      iconUrl: 'Assets/Icons/grondzeiler.png',
      iconSize: [32, 32],
      iconAnchor: [16, 32],
      popupAnchor: [0, -32]
    });

    this.molens.forEach(molen => {
      if (this.map) {
        const marker = L.marker([molen.north, molen.east], { icon: customIcon }).addTo(this.map);

        marker.on('click', () => {
          this.onMarkerClick(molen.ten_Brugge_Nr);
        });
      }
    });
    this.mapChange.emit(this.map);
  }

  private onMarkerClick(tenBruggeNr: any): void {
    const dialogRef = this.dialog.open(MolenDialogComponent, {
      data: { tenBruggeNr },
      panelClass: 'molen-details'
    });
  }
}
