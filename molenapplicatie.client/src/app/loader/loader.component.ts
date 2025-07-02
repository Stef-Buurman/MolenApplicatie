import { ChangeDetectorRef, Component, Input, NgZone, OnInit } from '@angular/core';
import { SharedDataService } from '../../Services/SharedDataService';

@Component({
  selector: 'app-loader',
  templateUrl: './loader.component.html',
  styleUrl: './loader.component.scss'
})
export class LoaderComponent implements OnInit {
  isLoadingVisible: boolean = true;
  isLoading: boolean = true;
  @Input() TimeToWait!: number;

  constructor(
    public sharedData: SharedDataService,
    private ngZone: NgZone,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.sharedData.IsLoading$.subscribe({
      next: (value) => {
        this.isLoading = value;
        if (this.isLoading) {
          this.showLoader();
        } else {
          this.fadeOutLoader();
        }
      },
    });

    if (this.TimeToWait) {
      setTimeout(() => {
        this.isLoadingVisible = false;
        this.cdr.detectChanges();
      }, this.TimeToWait + 500);
    }
  }

  showLoader() {
    this.isLoadingVisible = true;
    this.cdr.detectChanges();
  }

  fadeOutLoader() {
    setTimeout(() => {
      this.ngZone.run(() => {
        this.isLoadingVisible = false;
        this.cdr.detectChanges();
      });
    }, 500);
  }
}
