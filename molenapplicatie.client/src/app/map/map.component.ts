import { Component } from "@angular/core";
import { MolenDataClass } from "../../Class/MolenDataClass";
import { ErrorService } from "../../Services/ErrorService";
import { MapService } from "../../Services/MapService";
import { MolenService } from "../../Services/MolenService";
import { SharedDataService } from "../../Services/SharedDataService";
import { Toasts } from "../../Utils/Toasts";

@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrl: './map.component.scss'
})
export class MapComponent {
  molens: MolenDataClass[] = [];

  constructor(private toasts: Toasts,
    private errors: ErrorService,
    private sharedData: SharedDataService,
    private molenService: MolenService,
    private mapService: MapService) { }

  getMolens(): void {
    this.molenService.getAllMolens().subscribe({
      next: (result) => {
        this.molens = result;
        this.mapService.initMap(result);
      },
      error: (error) => {
        this.errors.AddError(error);
        this.toasts.showError("De molens kunnen niet geladen worden!");
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
