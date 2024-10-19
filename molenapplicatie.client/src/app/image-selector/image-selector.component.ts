import { Component, EventEmitter, Input, Output, SecurityContext } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { ImageDialogComponent } from '../image-dialog/image-dialog.component';
import { MolenImage } from '../../Class/MolenImage';
import { GetSafeUrl } from '../../Utils/GetSafeUrl';
import { DialogReturnType } from '../../Interfaces/DialogReturnType';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';
import { HttpClient } from '@angular/common/http';
import { Toasts } from '../../Utils/Toasts';

@Component({
  selector: 'app-image-selector',
  templateUrl: './image-selector.component.html',
  styleUrl: './image-selector.component.scss'
})
export class ImageSelectorComponent {
  @Input() images: MolenImage[] = [];
  @Output() imagesChange = new EventEmitter<MolenImage[]>();
  @Input() selectedImage?: MolenImage;
  @Output() selectedImageChange = new EventEmitter<MolenImage>();
  @Input() tbNr: string = "";

  constructor(private sanitizer: DomSanitizer,
    private dialog: MatDialog,
    private toast: Toasts,
    private http: HttpClient) { }

  ngOnInit(): void {
    if (this.images.length > 0) this.selectedImageChange.emit(this.images[0]);
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
          selectedImage
        },
        panelClass: 'selected-image'
      });

      dialogRef.afterClosed().subscribe(
        (result: DialogReturnType) => {
          if (result.status == DialogReturnStatus.Deleted) {
            if (this.deleteImage(selectedImage.name)) {
              this.images = this.images.filter(x => x.name != selectedImage.name);
              this.imagesChange.emit(this.images);
              this.selectedImageChange.emit(this.images[0])
              this.toast.showSuccess("Image is deleted");
            }
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
        this.toast.showSuccess("Foto is verwijderd");
        return true;
      },
      error: (error) => {
        this.toast.showError(error.error.message);
      }
    });
    return false;
  }
}
