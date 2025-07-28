import { Component, Input, OnInit } from '@angular/core';
import { ErrorService } from '../../Services/ErrorService';
import { SharedDataService } from '../../Services/SharedDataService';
import { Toasts } from '../../Utils/Toasts';
import { MapData } from '../../Interfaces/Map/MapData';
import { MapService } from '../../Services/MapService';
import { MolenService } from '../../Services/MolenService';
import { FilterFormValues } from '../../Interfaces/Filters/Filter';
import { Place } from '../../Interfaces/Models/Place';
import { catchError, Observable, tap } from 'rxjs';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-map-page',
  templateUrl: './map-page.component.html',
  styleUrl: './map-page.component.scss',
})
export class MapPageComponent implements OnInit {
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

  constructor(
    private toasts: Toasts,
    private errors: ErrorService,
    private molenService: MolenService,
    private mapService: MapService,
    private sharedData: SharedDataService,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    this.mapService.SelectedMapId = this.mapPageId;
    this.getMolens().subscribe();
    this.route.url.subscribe(() => {
      const firstChild = this.route.firstChild;
      if (
        !(firstChild && firstChild.snapshot.paramMap.get('TenBruggeNumber'))
      ) {
        navigator.geolocation.getCurrentPosition((position) => {
          const latitude = position.coords.latitude;
          const longitude = position.coords.longitude;
          this.mapService.mapReady.then(() => {
            this.mapService.setView([latitude, longitude], 12);
          });
        });
      }
    });
  }

  getMolens(filters: FilterFormValues[] = []): Observable<MapData[]> {
    this.sharedData.IsLoadingTrue();
    return this.molenService.getMapData(filters).pipe(
      tap((result) => {
        this.molens = result;
        this.mapService.initMap(result);
      }),
      catchError((error) => {
        this.errors.AddError(error);
        this.toasts.showError('De allMolens kunnen niet geladen worden!');
        return [];
      }),
      tap({
        complete: () => {
          this.toasts.showSuccess('Molens zijn geladen!');
          this.sharedData.IsLoadingFalse();
        },
      })
    );
  }
}
