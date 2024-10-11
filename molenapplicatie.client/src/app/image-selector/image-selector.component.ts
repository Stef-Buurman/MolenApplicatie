import { Component, Input } from '@angular/core';
import { SafeUrl } from '@angular/platform-browser';

@Component({
  selector: 'app-image-selector',
  templateUrl: './image-selector.component.html',
  styleUrl: './image-selector.component.css'
})
export class ImageSelectorComponent {
  @Input() images: SafeUrl[] = [];

  public selectedImage?: SafeUrl;
  constructor() {}

  ngOnInit(): void {
    if (this.images.length > 0) {
      this.selectedImage = this.images[0];
    }
  }

  changeImage(imageUrl: SafeUrl) {
    this.selectedImage = imageUrl;
  }
}
