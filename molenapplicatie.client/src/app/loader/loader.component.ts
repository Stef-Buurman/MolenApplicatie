import { ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { SharedDataService } from '../../Services/SharedDataService';

@Component({
  selector: 'app-loader',
  templateUrl: './loader.component.html',
  styleUrl: './loader.component.scss'
})
export class LoaderComponent implements OnChanges {
  isLoadingVisible: boolean = true;
  isLoading: boolean = true;
  @Input() TimeToWait!: number;
  constructor(public sharedData: SharedDataService,
    private cdr: ChangeDetectorRef) { }

  ngOnChanges(changes: SimpleChanges): void {
    this.sharedData.IsLoading$.subscribe({
      next: (value) => {
        this.isLoading = value;
        setTimeout(() => {
          this.isLoadingVisible = value;
        }, 500);
      }
    });
  }

  ngOnInit() {
    if (this.TimeToWait) {
      setTimeout(() => {
        this.isLoadingVisible = false;
      }, this.TimeToWait + 500);
    }
    this.cdr.detectChanges();
  }
}
