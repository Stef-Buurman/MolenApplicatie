import { AfterViewInit, Component } from '@angular/core';
import { ErrorService } from '../../Services/ErrorService';
import { MapService } from '../../Services/MapService';
import { MolenService } from '../../Services/MolenService';
import { SharedDataService } from '../../Services/SharedDataService';
import { Toasts } from '../../Utils/Toasts';
import { MolenData } from '../../Interfaces/MolenData';

@Component({
  selector: 'app-map-existing-molens',
  templateUrl: './map-existing-molens.component.html',
  styleUrl: './map-existing-molens.component.scss'
})
export class MapExistingMolensComponent implements AfterViewInit {
  molens: MolenData[] = [];
  mapId: string = "existingMolensMap"

  constructor(private toasts: Toasts,
    private errors: ErrorService,
    private sharedData: SharedDataService,
    private molenService: MolenService,
    private mapService: MapService) { }

  getMolens(): void {
    this.molenService.getAllExistingMolens().subscribe({
      next: (result) => {
        this.molens = result;
        this.mapService.SelectedMapId = this.mapId;
        this.mapService.initMap(result);
      },
      error: (error) => {
        this.errors.AddError(error);
        this.toasts.showError("De allMolens kunnen niet geladen worden!");
      },
      complete: () => {
        this.toasts.showSuccess("Molens zijn geladen!");
        this.sharedData.IsLoadingFalse();
      }
    });
  }

  ngAfterViewInit(): void {
    this.sharedData.IsLoadingTrue();
    this.getMolens();
  }
}
