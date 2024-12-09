import { AfterViewInit, Component } from '@angular/core';
import { MolenDataClass } from '../../Class/MolenDataClass';
import { ErrorService } from '../../Services/ErrorService';
import { MapService } from '../../Services/MapService';
import { MolenService } from '../../Services/MolenService';
import { SharedDataService } from '../../Services/SharedDataService';
import { Toasts } from '../../Utils/Toasts';

@Component({
  selector: 'app-map-active-molens',
  templateUrl: './map-active-molens.component.html',
  styleUrl: './map-active-molens.component.scss'
})
export class MapActiveMolensComponent implements AfterViewInit{
  molens: MolenDataClass[] = [];
  mapId:string = "activeMolensMap"

  constructor(private toasts: Toasts,
    private errors: ErrorService,
    private sharedData: SharedDataService,
    private molenService: MolenService,
    private mapService: MapService) { }

  getMolens(): void {
    this.molenService.getAllActiveMolens().subscribe({
      next: (result) => {
        this.molens = result;
        this.mapService.SelectedMapId = this.mapId;
        //if (!this.mapService.doesMapIdExist(this.mapId)) {
          this.mapService.initMap(result);
        //}
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
