import { Component, Input, SecurityContext } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { ImageDialogComponent } from '../image-dialog/image-dialog.component';
import { MolenImage } from '../../Class/MolenImage';
import { GetSafeUrl } from '../../Utils/GetSafeUrl';
import { DialogReturnType } from '../../Interfaces/DialogReturnType';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';
import { HttpClient } from '@angular/common/http';import { Toasts } from '../../Utils/Toasts';

@Component({
  selector: 'app-image-selector',
  templateUrl: './image-selector.component.html',
  styleUrl: './image-selector.component.css'
})
export class ImageSelectorComponent {
  @Input() images: MolenImage[] = [];
  @Input() tbNr: string = "";

  public selectedImage?: MolenImage;
  constructor(private sanitizer: DomSanitizer,
    private dialog: MatDialog,
    private toast: Toasts,
    private http: HttpClient) { }

  ngOnInit(): void {
    if (this.images.length > 0) this.selectedImage = this.images[0];
  }

  getImageByName(name: string): MolenImage | undefined {
    return this.images.find(x => x.name == name);
  }

  changeImage(imgName: string) {
    var newSelectedImage = this.getImageByName(imgName);

    if (newSelectedImage != undefined) {
      this.selectedImage = newSelectedImage;
    }
  }

  isSelectedImage(image: MolenImage): Boolean {
    if (!this.selectedImage) return false;
    return this.selectedImage.name == image.name;
  }

  openImage(): void {
    if (this.selectedImage) {
      this.selectedImage.image = this.getSafeUrl(this.selectedImage.name);
      var selectedImage: MolenImage = this.selectedImage;

      const dialogRef = this.dialog.open(ImageDialogComponent, {
        data: {
          selectedImage,
          onDelete: (name: string) => this.deleteImage(name)
        },
        panelClass: 'selected-image'
      });

      dialogRef.afterClosed().subscribe(
        (result: DialogReturnType) => {
          if (result.status == DialogReturnStatus.Success) {
            this.images = this.images.filter(x => x.name != selectedImage.name);
            this.selectedImage = this.images[0];
            this.toast.showSuccess("Image is deleted");
          }
          else if (result.status == DialogReturnStatus.Error) {
            this.toast.showError("Error while deleting image");
          }
        }
      )
    }
  }

  getSafeUrl(imgName: string): SafeUrl | undefined {
    var image = this.getImageByName(imgName);

    if (image != undefined) {
      return GetSafeUrl(this.sanitizer, image.content);
    }
    return undefined;
  }

  deleteImage(imageName: string): boolean {
    this.http.delete("/api/molen_image/" + this.tbNr + "/" + imageName).subscribe({
      next: (result) => {
        this.images = this.images.filter(x => x.name != imageName);
        this.selectedImage = this.images[0];
        this.toast.showSuccess("Image is deleted");
        return true;
      },
      error: (error) => {
        console.log(error);
        this.toast.showError("Error while deleting image");
      }
    });
    return false;
  }
}