import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, Input } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';
import { ConfirmationDialogData } from '../../Interfaces/ConfirmationDialogData';
import { DialogReturnType } from '../../Interfaces/DialogReturnType';
import { MolenData } from '../../Interfaces/Models/MolenData';
import { Place } from '../../Interfaces/Models/Place';
import { ErrorService } from '../../Services/ErrorService';
import { Toasts } from '../../Utils/Toasts';
import { ConfirmationDialogComponent } from '../dialogs/confirmation-dialog/confirmation-dialog.component';
import { FilterMapComponent } from '../dialogs/filter-map/filter-map.component';
import { FilterFormValues } from '../../Interfaces/Filters/Filter';
import { MapService } from '../../Services/MapService';
import { MolenService } from '../../Services/MolenService';
import { MolenType } from '../../Interfaces/Models/MolenType';
import { Observable } from 'rxjs';
import { MapData } from '../../Interfaces/Map/MapData';
import { SharedDataService } from '../../Services/SharedDataService';

@Component({
  selector: 'layout',
  templateUrl: './root.component.html',
  styleUrl: './root.component.scss',
})
export class RootComponent {
  visible: boolean = false;
  selectedTenBruggeNumber: string | undefined;
  selectedPlace!: Place;

  private NewMolensLastExecutionTime: number | null = null;
  private UpdateLastExecutionTime: number | null = null;
  private readonly cooldownTime = 10 * 60 * 1000;
  private currentFilters: FilterFormValues[] = [];

  get error(): boolean {
    return this.errors.HasError;
  }

  get getMolenWithImageAmount(): number | undefined {
    return this.molenService.molensWithImageAmount;
  }

  @Input() onFilterChange!: (
    filters: FilterFormValues[]
  ) => Observable<MapData[]>;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private toasts: Toasts,
    private http: HttpClient,
    private dialog: MatDialog,
    private errors: ErrorService,
    private molenService: MolenService,
    private sharedData: SharedDataService,
    private mapService: MapService
  ) {}

  onPlaceChange(selectedPlace: Place) {
    if (!selectedPlace && this.selectedPlace) return;
    else if (!selectedPlace) selectedPlace = this.selectedPlace;
    var zoom: number = 13;
    if (selectedPlace.population == 0) zoom = 15;
    this.mapService.setView(
      [selectedPlace.latitude, selectedPlace.longitude],
      zoom
    );
  }

  onMolenChange(selectedMolen: MolenData) {
    if (!selectedMolen) return;
    this.router.navigate([selectedMolen.ten_Brugge_Nr], {
      relativeTo: this.route,
    });
    if (
      !this.mapService.doesTenBruggeNumberExist(selectedMolen.ten_Brugge_Nr)
    ) {
      let newMolenState: string = '';
      if (
        selectedMolen.toestand?.toLowerCase() === 'restant' ||
        selectedMolen.toestand?.toLowerCase() === 'in aanbouw'
      ) {
        newMolenState = 'Bestaande';
      } else if (selectedMolen.toestand?.toLowerCase() === 'werkend') {
        newMolenState = 'Werkend';
        if (this.currentFilters.find((f) => f.filterName === 'MolenType')) {
          this.currentFilters = this.currentFilters.filter(
            (f) => f.filterName !== 'MolenType'
          );
        }
        if (this.currentFilters.find((f) => f.filterName === 'Provincie')) {
          this.currentFilters = this.currentFilters.filter(
            (f) => f.filterName !== 'Provincie'
          );
        }
      }
      if (this.currentFilters.find((f) => f.filterName === 'MolenState')) {
        this.currentFilters = this.currentFilters.filter(
          (f) => f.filterName !== 'MolenState'
        );
        this.currentFilters.push({
          filterName: 'MolenState',
          value: newMolenState,
          isAList: false,
          type: 'string',
          name: 'MolenState',
        });
      } else {
        this.currentFilters.push({
          filterName: 'MolenState',
          value: newMolenState,
          isAList: false,
          type: 'string',
          name: 'MolenState',
        });
      }
      this.onFilterChange(this.currentFilters).subscribe({
        complete: () => {
          setTimeout(() => {
            this.mapService.setView(
              [selectedMolen.latitude, selectedMolen.longitude],
              15
            );
          }, 0);
        },
      });
    } else {
      this.mapService.setView(
        [selectedMolen.latitude, selectedMolen.longitude],
        15
      );
    }
  }

  onTypeChange(selectedType: MolenType) {
    if (this.currentFilters.find((f) => f.filterName === 'MolenType')) {
      this.currentFilters = this.currentFilters.filter(
        (f) => f.filterName !== 'MolenType'
      );
      this.currentFilters.push({
        filterName: 'MolenType',
        value: selectedType.name,
        isAList: false,
        type: 'string',
        name: 'MolenType',
      });
    } else {
      this.currentFilters.push({
        filterName: 'MolenType',
        value: selectedType.name,
        isAList: false,
        type: 'string',
        name: 'MolenType',
      });
    }
    this.changeFilters();
  }

  openInfoMenu() {
    this.visible = !this.visible;
  }

  filterMap() {
    const dialogRef = this.dialog.open(FilterMapComponent, {
      panelClass: 'filter-map',
      data: {
        filters: this.currentFilters,
      },
    });

    dialogRef.afterClosed().subscribe({
      next: (result: FilterFormValues[] | undefined) => {
        if (result) {
          this.currentFilters = result;
          this.changeFilters();
        }
      },
    });
  }

  changeFilters() {
    this.onFilterChange(this.currentFilters).subscribe({
      next: (mapData: MapData[]) => {
        if (mapData.length === 0) {
          if (
            this.currentFilters.find((f) => f.filterName === 'MolenState') ==
              null ||
            (typeof this.currentFilters.find(
              (f) => f.filterName === 'MolenState'
            )?.value === 'string' &&
              (
                this.currentFilters.find((f) => f.filterName === 'MolenState')
                  ?.value as string
              ).toLowerCase() !== 'verdwenen')
          ) {
            this.currentFilters = this.currentFilters.filter(
              (f) => f.filterName !== 'MolenState'
            );
            this.currentFilters.push({
              filterName: 'MolenState',
              value: 'Verdwenen',
              isAList: false,
              type: 'string',
              name: 'MolenState',
            });
          } else if (
            this.currentFilters.find((f) => f.filterName === 'Provincie') !==
            null
          ) {
            this.currentFilters = this.currentFilters.filter(
              (f) => f.filterName !== 'Provincie'
            );
          }
          this.changeFilters();
        }
      },
    });
  }

  updateMolens() {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      panelClass: 'update-molens-dialog',
      data: {
        title: 'Molens updaten',
        message: 'Weet je zeker dat je de oudste molens wilt updaten?',
        api_key_usage: true,
      } as ConfirmationDialogData,
    });

    dialogRef.afterClosed().subscribe({
      next: (result: DialogReturnType) => {
        var previousLastExcecutionTime = this.UpdateLastExecutionTime;
        if (result.status == DialogReturnStatus.Confirmed && result.api_key) {
          const headers = new HttpHeaders({
            Authorization: result.api_key,
          });

          const currentTime = Date.now();
          if (
            this.UpdateLastExecutionTime &&
            currentTime - this.UpdateLastExecutionTime < this.cooldownTime
          ) {
            this.toasts.showWarning('Dit kan eens elke 30 minuten!');
            return;
          }

          var isDone: boolean = false;

          this.toasts.showInfo(
            'Molens worden geupdate... (Dit kan even duren)'
          );

          this.http
            .get<MolenData[]>('/api/molen/update_oldest_molens', { headers })
            .subscribe({
              next: (result) => {
                this.toasts.showSuccess(
                  'Er zijn ' + result.length + ' molens geupdate.'
                );
              },
              error: (error) => {
                isDone = true;
                if (error.status == 401) {
                  this.toasts.showError(
                    'Je hebt een verkeerde api_key ingevuld!'
                  );
                } else if (error) {
                  this.toasts.showError(error.error);
                }

                this.UpdateLastExecutionTime = previousLastExcecutionTime;
              },
              complete: () => {
                isDone = true;
              },
            });

          setTimeout(() => {
            if (!isDone) {
              this.toasts.showInfo('Jaja, ik ben nog bezig voor je.');
            }
          }, 15000);

          this.UpdateLastExecutionTime = currentTime;
        } else if (
          result.status == DialogReturnStatus.Confirmed &&
          !result.api_key
        ) {
          this.toasts.showWarning(
            'Er is geen api key ingevuld, er is niets gebeurt!'
          );
        } else if (result.status == DialogReturnStatus.Error) {
          this.toasts.showError(
            'Er is iets fout gegaan met het updaten van de molens!'
          );
        }
      },
    });
  }

  searchForNewMolens() {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      panelClass: 'search-new-molens-dialog',
      data: {
        title: 'Nieuwe molens',
        message: 'Weet je zeker dat je voor nieuwe molens wilt zoeken?',
        api_key_usage: true,
      } as ConfirmationDialogData,
    });

    dialogRef.afterClosed().subscribe({
      next: (result: DialogReturnType) => {
        if (result.status == DialogReturnStatus.Confirmed && result.api_key) {
          const headers = new HttpHeaders({
            Authorization: result.api_key,
          });

          const currentTime = Date.now();
          if (
            this.NewMolensLastExecutionTime &&
            currentTime - this.NewMolensLastExecutionTime < this.cooldownTime
          ) {
            this.toasts.showWarning('Dit kan eens elke 60 minuten!');
            return;
          }

          var isDone: boolean = false;

          this.toasts.showInfo(
            'Nieuwe molens worden gezocht... (Dit kan even duren)'
          );

          this.http
            .get<MolenData[]>('/api/molen/search_for_new_molens', { headers })
            .subscribe({
              next: (result: MolenData[]) => {
                if (result.length == 0) {
                  this.toasts.showInfo('Er zijn geen nieuwe molens gevonden!');
                } else if (result.length == 1) {
                  this.toasts.showSuccess(
                    'Er is ' + result.length + ' nieuwe molen gevonden!'
                  );
                  this.mapService.setView(
                    [result[0].latitude, result[0].longitude],
                    13
                  );
                } else {
                  this.toasts.showSuccess(
                    'Er zijn ' + result.length + ' nieuwe molens gevonden!'
                  );
                }
              },
              error: (error) => {
                isDone = true;
                if (error.status == 401) {
                  this.toasts.showError(
                    'Je hebt een verkeerde api_key ingevuld!'
                  );
                } else if (error) {
                  this.toasts.showError(error.error);
                }

                this.UpdateLastExecutionTime = currentTime;
              },
              complete: () => {
                isDone = true;
              },
            });

          setTimeout(() => {
            if (!isDone) {
              this.toasts.showInfo('Jaja, ik ben nog bezig voor je.');
            }
          }, 15000);

          this.NewMolensLastExecutionTime = currentTime;
        } else if (
          result.status == DialogReturnStatus.Confirmed &&
          !result.api_key
        ) {
          this.toasts.showWarning(
            'Er is geen api key ingevuld, er is niets gebeurt!'
          );
        } else if (result.status == DialogReturnStatus.Error) {
          this.toasts.showError(
            'Er is iets fout gegaan met het updaten van de molens!'
          );
        }
      },
    });
  }
}
