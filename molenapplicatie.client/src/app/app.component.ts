import { Component, OnInit, ViewContainerRef } from '@angular/core';
import { Toasts } from '../Utils/Toasts';
import { ToastType } from '../Enums/ToastType';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { MolenData } from '../Interfaces/MolenData';
import { InitializeDataStatus } from '../Enums/InitializeDataStatus';
import { Place } from '../Interfaces/Place';
import { ConfirmationDialogData } from '../Interfaces/ConfirmationDialogData';
import { ConfirmationDialogComponent } from './confirmation-dialog/confirmation-dialog.component';
import { MatDialog } from '@angular/material/dialog';
import { DialogReturnType } from '../Interfaces/DialogReturnType';
import { DialogReturnStatus } from '../Enums/DialogReturnStatus';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  map: any;
  visible: boolean = false;
  error: boolean = false;
  status: InitializeDataStatus = InitializeDataStatus.Initial;
  InitializeDataStatus = InitializeDataStatus;
  isLoading = true;
  isLoadingVisible = true;

  selectedPlace!: Place;


  private NewMolensLastExecutionTime: number | null = null;
  private UpdateLastExecutionTime: number | null = null;
  private readonly cooldownTime = 10 * 60 * 1000;
  constructor(private toasts: Toasts,
    private vcr: ViewContainerRef,
    private http: HttpClient,
    private dialog: MatDialog) { }

  ngOnInit() {
    this.toasts.setViewContainerRef(this.vcr);
    setTimeout(() => {
      this.isLoadingVisible = false;
    }, 1250);
  }

  onPlaceChange(selectedPlace: Place) {
    if (!selectedPlace && this.selectedPlace) return;
    else if (!selectedPlace) selectedPlace = this.selectedPlace;
    var zoom: Number = 13;
    if (selectedPlace.population == 0) zoom = 15;
    this.map.setView([selectedPlace.lat, selectedPlace.lon], zoom);
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
                this.map.setView([result[0].north, result[0].east], 13);
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
