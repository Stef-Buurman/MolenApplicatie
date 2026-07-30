import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter, startWith, Subject, takeUntil } from 'rxjs';
import { FilterFormValues } from '../../Interfaces/Filters/Filter';
import { RecentAddedImages } from '../../Interfaces/MolensResponseType';
import { MolenService } from '../../Services/MolenService';
import { Toasts } from '../../Utils/Toasts';
import { MolenClusteredMapComponent } from '../molen-clustered-map/molen-clustered-map.component';
import { MapLocation } from '../root/root.component';

@Component({
  selector: 'app-map-page',
  standalone: false,
  templateUrl: './map-page.component.html',
  styleUrl: './map-page.component.scss',
})
export class MapPageComponent implements OnInit, OnDestroy {
  @ViewChild(MolenClusteredMapComponent)
  private clusteredMap?: MolenClusteredMapComponent;

  public useCurrentLocation: boolean = false;
  public isPopupVisible: boolean = false;
  public recentAddedImages: RecentAddedImages[] = [];
  public molensWithImageAmount: number = 0;
  public filters: FilterFormValues[] = [
    {
      filterName: 'MolenState',
      value: 'Werkend',
      isAList: false,
      type: 'string',
      name: 'Toestand',
    },
  ];

  private readonly destroyed$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private molenService: MolenService,
    private toasts: Toasts,
  ) {}

  ngOnInit(): void {
    this.router.events
      .pipe(
        filter(
          (event): event is NavigationEnd => event instanceof NavigationEnd,
        ),
        startWith(null),
        takeUntil(this.destroyed$),
      )
      .subscribe(() => {
        this.useCurrentLocation = !this.hasOpenMolenDetailRoute();
      });

    this.molenService.mapSummary$
      .pipe(takeUntil(this.destroyed$))
      .subscribe((summary) => {
        this.molensWithImageAmount = summary.totalMolensWithImage;
        this.recentAddedImages = summary.recentAddedImages;
        this.isPopupVisible = summary.recentAddedImages.length > 0;
      });

    this.molenService
      .getMapSummary()
      .pipe(takeUntil(this.destroyed$))
      .subscribe({
        error: () => {
          this.toasts.showError('De kaartinformatie kon niet worden geladen.');
        },
      });
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  onFiltersChange(filters: FilterFormValues[]): void {
    this.filters = [...filters];
  }

  onMapLocationChange(location: MapLocation): void {
    this.clusteredMap?.setView(
      location.latitude,
      location.longitude,
      location.zoom,
    );
  }

  private hasOpenMolenDetailRoute(): boolean {
    let route: ActivatedRoute | null = this.route.firstChild;

    while (route) {
      if (route.snapshot.paramMap.has('MolenId')) {
        return true;
      }

      route = route.firstChild;
    }

    return false;
  }
}
