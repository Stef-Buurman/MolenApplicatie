import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';

@Component({
  selector: 'app-loader',
  templateUrl: './loader.component.html',
  styleUrl: './loader.component.scss'
})
export class LoaderComponent implements OnChanges {
  @Input() isLoadingVisible: boolean = true;
  @Input() isLoading: boolean = true;
  @Input() TimeToWait: number = 1000;

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.isLoadingVisible) {
      setTimeout(() => {
        this.isLoading = !this.isLoading;
      }, 500);
    }
  }
}
