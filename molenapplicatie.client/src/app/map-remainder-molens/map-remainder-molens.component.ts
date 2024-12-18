import { AfterViewInit, Component } from '@angular/core';
import { MolenDataClass } from '../../Class/MolenDataClass';
import { ErrorService } from '../../Services/ErrorService';
import { MapService } from '../../Services/MapService';
import { MolenService } from '../../Services/MolenService';
import { SharedDataService } from '../../Services/SharedDataService';
import { Toasts } from '../../Utils/Toasts';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-map-remainder-molens',
  templateUrl: './map-remainder-molens.component.html',
  styleUrl: './map-remainder-molens.component.scss'
})
export class MapRemainderMolensComponent implements AfterViewInit {
  molens: MolenDataClass[] = [];
  mapId: string = "remainderMolensMap"

  constructor(private toasts: Toasts,
    private errors: ErrorService,
    private sharedData: SharedDataService,
    private molenService: MolenService,
    private mapService: MapService,
    private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      //this.selectedTenBruggeNumber = params.get('TenBruggeNumber') || '';
      //if (this.selectedTenBruggeNumber) {
      //  this.molenService.selectedMolenTenBruggeNumber = this.selectedTenBruggeNumber;
      //  this.OpenMolenDialog(this.selectedTenBruggeNumber);
      //}
      //console.log(params.get('TenBruggeNumber'))
    });
  }

  getMolens(): void {
    this.molenService.getAllRemainderMolens().subscribe({
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
