import { Component, Input } from '@angular/core';
import { ErrorService } from '../../Services/ErrorService';
import { SharedDataService } from '../../Services/SharedDataService';
import { Toasts } from '../../Utils/Toasts';
import { MapData } from '../../Interfaces/Map/MapData';
import { MapService2_0 } from '../../Services/MapService2-0';
import { MolenService2_0 } from '../../Services/MolenService2_0';
import { FilterFormValues } from '../../Interfaces/Filters/Filter';
import { Place } from '../../Interfaces/Models/Place';

@Component({
  selector: 'app-map-page',
  templateUrl: './map-page.component.html',
  styleUrl: './map-page.component.scss',
})
export class MapPageComponent {
  molens: MapData[] = [];
  mapPageId: string = 'activeMolensMap';
  visible: boolean = false;
  selectedTenBruggeNumber: string | undefined;
  selectedPlace!: Place;

  get error(): boolean {
    return this.errors.HasError;
  }

  get getMolenWithImageAmount(): number | undefined {
    return this.molenService.molensWithImageAmount;
  }

  onFilterChanged = (filters: FilterFormValues[]) => {
    this.getMolens(filters);
  };

  constructor(
    private toasts: Toasts,
    private errors: ErrorService,
    private molenService: MolenService2_0,
    private mapService: MapService2_0,
    private sharedData: SharedDataService
  ) {}

  getMolens(filters: FilterFormValues[] = []): void {
    console.log(filters);
    this.sharedData.IsLoadingTrue();
    this.molenService.getMapData(filters).subscribe({
      next: (result) => {
        this.molens = result;
        this.mapService.SelectedMapId = this.mapPageId;
        this.mapService.initMap(result);
      },
      error: (error) => {
        this.errors.AddError(error);
        this.toasts.showError('De allMolens kunnen niet geladen worden!');
      },
      complete: () => {
        this.toasts.showSuccess('Molens zijn geladen!');
        this.sharedData.IsLoadingFalse();
      },
    });
  }

  ngAfterViewInit(): void {
    this.mapService.SelectedMapId = this.mapPageId;
    this.sharedData.IsLoadingTrue();
    this.getMolens();
  }
}
