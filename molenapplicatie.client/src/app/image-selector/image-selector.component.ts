import { Component, EventEmitter, Input, Output, SecurityContext } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { ImageDialogComponent } from '../image-dialog/image-dialog.component';
import { MolenImage } from '../../Class/MolenImage';
import { GetSafeUrl } from '../../Utils/GetSafeUrl';
import { DialogReturnType } from '../../Interfaces/DialogReturnType';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Toasts } from '../../Utils/Toasts';
import { catchError, Observable, Subscription, tap, throwError } from 'rxjs';

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
      var canBeDeleted: boolean = selectedImage.canBeDeleted;

      const dialogRef = this.dialog.open(ImageDialogComponent, {
        data: {
          selectedImage,
          canBeDeleted
        },
        panelClass: 'selected-image'
      });

      dialogRef.afterClosed().subscribe(
        (result: DialogReturnType) => {
          console.log(result);
          if (result.status == DialogReturnStatus.Deleted && result.api_key) {
            this.deleteImage(selectedImage.name, result.api_key).subscribe({
              next: (result) => {
                this.imagesChange.emit(this.images);
                this.selectedImageChange.emit(this.images[0]);
              },
              error: (error) => {
                if (error.status == 401) {
                  this.toast.showError("Er is een verkeerde api key ingevuld!");
                } else {
                  this.toast.showError(error.error.message);
                }
              }
            });
          }
          else if (result.status == DialogReturnStatus.Deleted && !result.api_key) {
            this.toast.showWarning("Er is geen api key ingevuld, de foto is niet verwijderd!");
          }
          else if (result.status == DialogReturnStatus.Error) {
            this.toast.showError("Er is iets fout gegaan met het verwijderen van de foto!");
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

  deleteImage(imageName: string, APIKey: string): Observable<any> {
    console.log(APIKey);
    const headers = new HttpHeaders({
      'Authorization': APIKey,
    });

    return this.http.delete("/api/molen_image/" + this.tbNr + "/" + imageName, { headers }).pipe(
      tap(() => {
        this.images = this.images.filter(x => x.name != imageName);
        this.selectedImage = this.images[0];
        this.toast.showSuccess("Foto is verwijderd");
      }),
      catchError((error) => {
        if (error.status == 401) {
          this.toast.showError("Er is een verkeerde api key ingevuld!");
        } else {
          this.toast.showError(error.error.message);
        }
        return throwError(error);
      })
    );
  }
}
