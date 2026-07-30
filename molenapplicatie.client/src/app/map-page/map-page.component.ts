import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter, startWith, Subject, takeUntil } from 'rxjs';
import { RecentAddedImages } from '../../Interfaces/MolensResponseType';

@Component({
  selector: 'app-map-page',
  standalone: false,
  templateUrl: './map-page.component.html',
  styleUrl: './map-page.component.scss',
})
export class MapPageComponent implements OnInit, OnDestroy {
  public useCurrentLocation: boolean = false;
  public isPopupVisible: boolean = false;
  public recentAddedImages: RecentAddedImages[] = [];

  private readonly destroyed$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
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
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  private hasOpenMolenDetailRoute(): boolean {
    let route: ActivatedRoute | null = this.route.firstChild;

    while (route) {
      if (route.snapshot.paramMap.has('TenBruggeNumber')) {
        return true;
      }

      route = route.firstChild;
    }

    return false;
  }
}
