import { Component, Input, SecurityContext } from '@angular/core';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';

@Component({
  selector: 'app-image-selector',
  templateUrl: './image-selector.component.html',
  styleUrl: './image-selector.component.css'
})
export class ImageSelectorComponent {
  @Input() images: SafeUrl[] = [];

  public selectedImage?: SafeUrl;
  constructor(private sanitizer: DomSanitizer) { }

  ngOnInit(): void {
    if (this.images.length > 0) {
      this.selectedImage = this.images[0];
    }
  }

  changeImage(imageUrl: SafeUrl) {
    this.selectedImage = imageUrl;
  }

  isSelectedImage(imageUrl: SafeUrl): Boolean {
    if (!this.selectedImage) return false;
    return this.sanitizer.sanitize(SecurityContext.URL, this.selectedImage) == this.sanitizer.sanitize(SecurityContext.URL, imageUrl);
  }
}
