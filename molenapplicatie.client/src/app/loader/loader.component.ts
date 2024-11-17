import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';

@Component({
  selector: 'app-loader',
  templateUrl: './loader.component.html',
  styleUrl: './loader.component.scss'
})
export class LoaderComponent implements OnChanges, OnInit {
  @Input() isLoadingVisible: boolean = true;
  @Input() isLoading: boolean = true;
  @Input() TimeToWait!: number;

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.isLoadingVisible) {
      setTimeout(() => {
        this.isLoading = !this.isLoading;
      }, 500);
    }
  }

  ngOnInit() {
    if (this.TimeToWait) {
      setTimeout(() => {
        this.isLoading = !this.isLoading;
      }, this.TimeToWait + 500);
    }
  }
}
