import { AfterViewInit, Component } from '@angular/core';
import { MolenDataClass } from '../../Class/MolenDataClass';
import { ErrorService } from '../../Services/ErrorService';
import { MapService } from '../../Services/MapService';
import { MolenService } from '../../Services/MolenService';
import { SharedDataService } from '../../Services/SharedDataService';
import { Toasts } from '../../Utils/Toasts';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-map-disappeared-molens',
  templateUrl: './map-disappeared-molens.component.html',
  styleUrl: './map-disappeared-molens.component.scss'
})
export class MapDisappearedMolensComponent implements AfterViewInit {
  molens: MolenDataClass[] = [];
  mapId: string = "disappearedMolensMap"
  private provincie!: string;

  constructor(private toasts: Toasts,
    private route: ActivatedRoute,
    private router: Router,
    private errors: ErrorService,
    private sharedData: SharedDataService,
    private molenService: MolenService,
    private mapService: MapService) { }

  getMolens(): void {
    this.molenService.getDisappearedMolensByProvincie(this.provincie).subscribe({
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
    this.route.paramMap.subscribe(params => {
      var provincie = params.get('provincie')
      if (provincie != null) {
        this.provincie = provincie;
        this.sharedData.IsLoadingTrue();
        this.getMolens();
      } else {
        this.toasts.showError("Je moet een provincie gekozen hebben!");
      }
    });
  }
}
