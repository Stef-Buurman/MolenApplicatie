import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, OnInit, ViewContainerRef } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute } from '@angular/router';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';
import { ConfirmationDialogData } from '../../Interfaces/ConfirmationDialogData';
import { DialogReturnType } from '../../Interfaces/DialogReturnType';
import { MolenData } from '../../Interfaces/MolenData';
import { Place } from '../../Interfaces/Place';
import { ErrorService } from '../../Services/ErrorService';
import { MapService } from '../../Services/MapService';
import { MolenService } from '../../Services/MolenService';
import { SharedDataService } from '../../Services/SharedDataService';
import { Toasts } from '../../Utils/Toasts';
import { ConfirmationDialogComponent } from '../confirmation-dialog/confirmation-dialog.component';


@Component({
  selector: 'app-map-root',
  templateUrl: './map-root.component.html',
  styleUrl: './map-root.component.scss'
})
export class MapRootComponent implements OnInit {
  visible: boolean = false;
  selectedTenBruggeNumber: string | undefined;
  selectedPlace!: Place;

  private NewMolensLastExecutionTime: number | null = null;
  private UpdateLastExecutionTime: number | null = null;
  private readonly cooldownTime = 10 * 60 * 1000;

  get error(): boolean {
    return this.errors.HasError;
  }

  constructor(private route: ActivatedRoute,
    private toasts: Toasts,
    private vcr: ViewContainerRef,
    private http: HttpClient,
    private dialog: MatDialog,
    private errors: ErrorService,
    private sharedData: SharedDataService,
    private molenService: MolenService,
    private mapService: MapService) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.selectedTenBruggeNumber = params.get('TenBruggeNumber') || '';
      if (this.selectedTenBruggeNumber) {
        this.molenService.selectedMolenTenBruggeNumber = this.selectedTenBruggeNumber;
      }
    });
  }

  onPlaceChange(selectedPlace: Place) {
    if (!selectedPlace && this.selectedPlace) return;
    else if (!selectedPlace) selectedPlace = this.selectedPlace;
    var zoom: number = 13;
    if (selectedPlace.population == 0) zoom = 15;
    this.mapService.setView([selectedPlace.lat, selectedPlace.lon], zoom);
  }

  openInfoMenu() {
    this.visible = !this.visible;
  }

  updateMolens() {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Molens updaten',
        message: 'Weet je zeker dat je de oudste molens wilt updaten?',
        api_key_usage: true
      } as ConfirmationDialogData
    });

    dialogRef.afterClosed().subscribe({
      next: (result: DialogReturnType) => {
        var previousLastExcecutionTime = this.UpdateLastExecutionTime;
        if (result.status == DialogReturnStatus.Confirmed && result.api_key) {

          const headers = new HttpHeaders({
            'Authorization': result.api_key,
          });

          const currentTime = Date.now();
          if (this.UpdateLastExecutionTime && currentTime - this.UpdateLastExecutionTime < this.cooldownTime) {
            this.toasts.showWarning("Dit kan eens elke 30 minuten!");
            return;
          }

          var isDone: boolean = false;

          this.toasts.showInfo("Molens worden geupdate... (Dit kan even duren)");

          this.http.get<MolenData[]>('/api/update_oldest_molens', { headers }).subscribe({
            next: (result) => {
              this.toasts.showSuccess("Er zijn " + result.length + " molens geupdate.");
            },
            error: (error) => {
              isDone = true;
              if (error.status == 401) {
                this.toasts.showError("Je hebt een verkeerde api_key ingevuld!");
              }
              else if (error) {
                this.toasts.showError(error.error);
              }

              this.UpdateLastExecutionTime = previousLastExcecutionTime;
            },
            complete: () => {
              isDone = true;
            }
          });

          setTimeout(() => {
            if (!isDone) {
              this.toasts.showInfo("Jaja, ik ben nog bezig voor je.");
            }
          }, 15000);

          this.UpdateLastExecutionTime = currentTime;
        }
        else if (result.status == DialogReturnStatus.Confirmed && !result.api_key) {
          this.toasts.showWarning("Er is geen api key ingevuld, er is niets gebeurt!");
        }
        else if (result.status == DialogReturnStatus.Error) {
          this.toasts.showError("Er is iets fout gegaan met het updaten van de molens!");
        }
      }
    })
  }

  searchForNewMolens() {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      data: {
        title: 'Nieuwe molens',
        message: 'Weet je zeker dat je voor nieuwe molens wilt zoeken?',
        api_key_usage: true
      } as ConfirmationDialogData
    });

    dialogRef.afterClosed().subscribe({
      next: (result: DialogReturnType) => {
        var previousLastExcecutionTime = this.UpdateLastExecutionTime;
        if (result.status == DialogReturnStatus.Confirmed && result.api_key) {

          const headers = new HttpHeaders({
            'Authorization': result.api_key,
          });

          const currentTime = Date.now();
          if (this.NewMolensLastExecutionTime && currentTime - this.NewMolensLastExecutionTime < this.cooldownTime) {
            this.toasts.showWarning("Dit kan eens elke 60 minuten!");
            return;
          }

          var isDone: boolean = false;

          this.toasts.showInfo("Nieuwe molens worden gezocht... (Dit kan even duren)");

          this.http.get<MolenData[]>('/api/search_for_new_molens', { headers }).subscribe({
            next: (result: MolenData[]) => {
              if (result.length == 0) {
                this.toasts.showInfo("Er zijn geen nieuwe molens gevonden!");
              }
              else if (result.length == 1) {
                this.toasts.showSuccess("Er is " + result.length + " nieuwe molen gevonden!");
                this.mapService.setView([result[0].north, result[0].east], 13);
              }
              else {
                this.toasts.showSuccess("Er zijn " + result.length + " nieuwe molens gevonden!");
              }
            },
            error: (error) => {
              isDone = true;
              if (error.status == 401) {
                this.toasts.showError("Je hebt een verkeerde api_key ingevuld!");
              }
              else if (error) {
                this.toasts.showError(error.error);
              }

              this.UpdateLastExecutionTime = currentTime;
            },
            complete: () => {
              isDone = true;
            }
          });

          setTimeout(() => {
            if (!isDone) {
              this.toasts.showInfo("Jaja, ik ben nog bezig voor je.");
            }
          }, 15000);

          this.NewMolensLastExecutionTime = currentTime;
        }
        else if (result.status == DialogReturnStatus.Confirmed && !result.api_key) {
          this.toasts.showWarning("Er is geen api key ingevuld, er is niets gebeurt!");
        }
        else if (result.status == DialogReturnStatus.Error) {
          this.toasts.showError("Er is iets fout gegaan met het updaten van de molens!");
        }
      }
    })
  }
}
