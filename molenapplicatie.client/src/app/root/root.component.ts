import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';
import { ConfirmationDialogData } from '../../Interfaces/ConfirmationDialogData';
import { DialogReturnType } from '../../Interfaces/DialogReturnType';
import { FilterFormValues } from '../../Interfaces/Filters/Filter';
import { MolenData } from '../../Interfaces/Models/MolenData';
import { MolenType } from '../../Interfaces/Models/MolenType';
import { Place } from '../../Interfaces/Models/Place';
import { RecentAddedImages } from '../../Interfaces/MolensResponseType';
import { SearchModelWithCount } from '../../Interfaces/SearchResultModel';
import { ErrorService } from '../../Services/ErrorService';
import { MolenService } from '../../Services/MolenService';
import { getTypedApiErrorMessage } from '../../Utils/TypedApiObservable';
import { Toasts } from '../../Utils/Toasts';
import { ConfirmationDialogComponent } from '../dialogs/confirmation-dialog/confirmation-dialog.component';
import { FilterMapComponent } from '../dialogs/filter-map/filter-map.component';

export interface MapLocation {
  latitude: number;
  longitude: number;
  zoom: number;
}

@Component({
  selector: 'layout',
  standalone: false,
  templateUrl: './root.component.html',
  styleUrl: './root.component.scss',
})
export class RootComponent {
  visible: boolean = false;
  selectedPlace!: Place;

  @Input() recentAddedImages: RecentAddedImages[] = [];
  @Input() isPopupVisible: boolean = false;
  @Input() molensWithImageAmount: number = 0;

  @Output() filtersChange = new EventEmitter<FilterFormValues[]>();
  @Output() mapLocationChange = new EventEmitter<MapLocation>();

  private newMolensLastExecutionTime: number | null = null;
  private updateLastExecutionTime: number | null = null;
  private readonly updateCooldownTime = 30 * 60 * 1000;
  private readonly newMolensCooldownTime = 60 * 60 * 1000;
  private currentFilters: FilterFormValues[] = [
    {
      filterName: 'MolenState',
      value: 'Werkend',
      isAList: false,
      type: 'string',
      name: 'Toestand',
    },
  ];

  get error(): boolean {
    return this.errors.HasError;
  }

  constructor(
    private router: Router,
    private toasts: Toasts,
    private dialog: MatDialog,
    private errors: ErrorService,
    private molenService: MolenService,
  ) {}

  onPlaceChange(selectedPlace: Place): void {
    if (!selectedPlace) return;

    this.selectedPlace = selectedPlace;
    this.mapLocationChange.emit({
      latitude: selectedPlace.latitude,
      longitude: selectedPlace.longitude,
      zoom: selectedPlace.population === 0 ? 15 : 13,
    });
  }

  onMolenChange(selectedMolen: MolenData, navigate: boolean = true): void {
    if (!selectedMolen) return;

    this.mapLocationChange.emit({
      latitude: selectedMolen.latitude,
      longitude: selectedMolen.longitude,
      zoom: 14,
    });

    if (navigate) {
      void this.router.navigate(['/map', selectedMolen.id]);
    }

    this.visible = false;
  }

  onTypeChange(selectedType: SearchModelWithCount<MolenType>): void {
    this.currentFilters = this.currentFilters.filter(
      (filter) => filter.filterName !== 'MolenType',
    );

    this.currentFilters.push({
      filterName: 'MolenType',
      value: selectedType.data.name,
      isAList: false,
      type: 'string',
      name: 'MolenType',
    });

    this.changeFilters();
  }

  openInfoMenu(): void {
    this.visible = !this.visible;
  }

  filterMap(): void {
    const dialogRef = this.dialog.open(FilterMapComponent, {
      panelClass: 'filter-map',
      data: {
        filters: this.currentFilters,
      },
    });

    dialogRef.afterClosed().subscribe({
      next: (result: FilterFormValues[] | undefined) => {
        if (result !== undefined) {
          this.currentFilters = result;
          this.changeFilters();
        }
      },
    });
  }

  changeFilters(): void {
    this.filtersChange.emit([...this.currentFilters]);
    this.visible = false;
  }

  updateMolens(): void {
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
        if (result.status !== DialogReturnStatus.Confirmed) return;

        if (!result.api_key) {
          this.toasts.showWarning(
            'Er is geen api key ingevuld, er is niets gebeurd!',
          );
          return;
        }

        const currentTime = Date.now();
        if (
          this.updateLastExecutionTime &&
          currentTime - this.updateLastExecutionTime < this.updateCooldownTime
        ) {
          this.toasts.showWarning('Dit kan eens elke 30 minuten!');
          return;
        }

        const previousExecutionTime = this.updateLastExecutionTime;
        this.updateLastExecutionTime = currentTime;
        this.toasts.showInfo(
          'Molens worden bijgewerkt... (Dit kan even duren)',
        );

        let isDone = false;
        this.molenService.updateOldestMolens(result.api_key).subscribe({
          next: (molens) => {
            this.toasts.showSuccess(
              `Er zijn ${molens.length} molens bijgewerkt.`,
            );
          },
          error: (error) => {
            isDone = true;
            this.updateLastExecutionTime = previousExecutionTime;

            if (error.status === 401) {
              this.toasts.showError('Je hebt een verkeerde api key ingevuld!');
              return;
            }

            this.toasts.showError(getTypedApiErrorMessage(error));
          },
          complete: () => {
            isDone = true;
          },
        });

        setTimeout(() => {
          if (!isDone) {
            this.toasts.showInfo('De molens worden nog bijgewerkt.');
          }
        }, 15000);
      },
    });
  }

  searchForNewMolens(): void {
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
        if (result.status !== DialogReturnStatus.Confirmed) return;

        if (!result.api_key) {
          this.toasts.showWarning(
            'Er is geen api key ingevuld, er is niets gebeurd!',
          );
          return;
        }

        const currentTime = Date.now();
        if (
          this.newMolensLastExecutionTime &&
          currentTime - this.newMolensLastExecutionTime <
            this.newMolensCooldownTime
        ) {
          this.toasts.showWarning('Dit kan eens per 60 minuten!');
          return;
        }

        const previousExecutionTime = this.newMolensLastExecutionTime;
        this.newMolensLastExecutionTime = currentTime;
        this.toasts.showInfo(
          'Nieuwe molens worden gezocht... (Dit kan even duren)',
        );

        let isDone = false;
        this.molenService.searchForNewMolens(result.api_key).subscribe({
          next: (molens) => {
            if (molens.length === 0) {
              this.toasts.showInfo('Er zijn geen nieuwe molens gevonden!');
            } else if (molens.length === 1) {
              this.toasts.showSuccess('Er is 1 nieuwe molen gevonden!');
              this.mapLocationChange.emit({
                latitude: molens[0].latitude,
                longitude: molens[0].longitude,
                zoom: 13,
              });
            } else {
              this.toasts.showSuccess(
                `Er zijn ${molens.length} nieuwe molens gevonden!`,
              );
            }
          },
          error: (error) => {
            isDone = true;
            this.newMolensLastExecutionTime = previousExecutionTime;

            if (error.status === 401) {
              this.toasts.showError('Je hebt een verkeerde api key ingevuld!');
              return;
            }

            this.toasts.showError(getTypedApiErrorMessage(error));
          },
          complete: () => {
            isDone = true;
          },
        });

        setTimeout(() => {
          if (!isDone) {
            this.toasts.showInfo('Er wordt nog naar nieuwe molens gezocht.');
          }
        }, 15000);
      },
    });
  }
}
