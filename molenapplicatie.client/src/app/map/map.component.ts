import { Component, AfterViewInit, input, Input, EventEmitter, Output } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as L from 'leaflet';
import { MolenDataClass } from '../../Class/MolenDataClass';
import { MatDialog } from '@angular/material/dialog';
import { MolenDialogComponent } from '../molen-dialog/molen-dialog.component';
import { Toasts } from '../../Utils/Toasts';
import { InitializeDataStatus } from '../../Enums/InitializeDataStatus';
import { MarkerInfo } from '../../Interfaces/MarkerInfo';
import { DialogReturnType } from '../../Interfaces/DialogReturnType';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';
import { MolenDialogReturnType } from '../../Interfaces/MolenDialogReturnType';

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
  markers: MarkerInfo[] = [];

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

    this.molens.forEach(molen => {
      this.addMarker(molen);

    });
    this.mapChange.emit(this.map);
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
        this.onMarkerClick(molen.ten_Brugge_Nr);
      });

      this.markers.push({ marker, tenBruggeNumber: molen.ten_Brugge_Nr });
    }
  }

  private onMarkerClick(tenBruggeNr: string): void {
    const dialogRef = this.dialog.open(MolenDialogComponent, {
      data: { tenBruggeNr },
      panelClass: 'molen-details'
    });

    dialogRef.afterClosed().subscribe({
      next: (result: MolenDialogReturnType) => {
        var molen = this.molens.find(molen => molen.ten_Brugge_Nr === tenBruggeNr);
        var marker = this.markers.find(marker => marker.tenBruggeNumber === tenBruggeNr);
        if (molen) {
          if (result.MolenImages.length > 1 || (result.MolenImages.length == 1 && result.MolenImages[0].name != tenBruggeNr)) {
            molen.hasImage = true;
          }
          else {
            molen.hasImage = false;
          }
          molen.addedImages = result.MolenImages;
          if (marker) {
            marker.marker.remove();
          }
          if (molen) this.addMarker(molen);
          this.mapChange.emit(this.map);
        }
      }
    });
  }
}
