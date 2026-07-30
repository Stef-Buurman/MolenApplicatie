import {
  ChangeDetectorRef,
  Component,
  Input,
  NgZone,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { Subscription } from 'rxjs';
import { SharedDataService } from '../../Services/SharedDataService';

@Component({
  selector: 'app-loader',
  standalone: false,
  templateUrl: './loader.component.html',
  styleUrl: './loader.component.scss',
})
export class LoaderComponent implements OnInit, OnDestroy {
  isLoadingVisible: boolean = false;
  isLoading: boolean = false;
  @Input() TimeToWait?: number;

  private loadingSubscription?: Subscription;
  private hideTimeout?: ReturnType<typeof setTimeout>;
  private maximumWaitTimeout?: ReturnType<typeof setTimeout>;

  constructor(
    public sharedData: SharedDataService,
    private ngZone: NgZone,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.loadingSubscription = this.sharedData.IsLoading$.subscribe({
      next: (value) => {
        if (value) {
          this.showLoader();
        } else {
          this.fadeOutLoader();
        }
      },
    });

    if (this.TimeToWait && this.TimeToWait > 0) {
      this.maximumWaitTimeout = setTimeout(() => {
        this.sharedData.IsLoadingFalse();
      }, this.TimeToWait);
    }
  }

  ngOnDestroy(): void {
    this.loadingSubscription?.unsubscribe();

    if (this.hideTimeout) {
      clearTimeout(this.hideTimeout);
    }

    if (this.maximumWaitTimeout) {
      clearTimeout(this.maximumWaitTimeout);
    }
  }

  private showLoader(): void {
    if (this.hideTimeout) {
      clearTimeout(this.hideTimeout);
      this.hideTimeout = undefined;
    }

    this.isLoading = true;
    this.isLoadingVisible = true;
    this.cdr.detectChanges();
  }

  private fadeOutLoader(): void {
    this.isLoading = false;

    if (!this.isLoadingVisible) {
      return;
    }

    if (this.hideTimeout) {
      clearTimeout(this.hideTimeout);
    }

    this.hideTimeout = setTimeout(() => {
      this.ngZone.run(() => {
        this.isLoadingVisible = false;
        this.hideTimeout = undefined;
        this.cdr.detectChanges();
      });
    }, 500);
  }
}
