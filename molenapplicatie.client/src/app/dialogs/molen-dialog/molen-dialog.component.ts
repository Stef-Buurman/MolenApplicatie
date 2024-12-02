import { ChangeDetectorRef, Component, Inject, OnDestroy } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Observable, of } from 'rxjs';
import { MolenDataClass } from '../../../Class/MolenDataClass';
import { MolenImage } from '../../../Class/MolenImage';
import { MolenData } from '../../../Interfaces/MolenData';
import { MolenService } from '../../../Services/MolenService';
import { Toasts } from '../../../Utils/Toasts';

@Component({
  selector: 'app-molen-dialog',
  templateUrl: './molen-dialog.component.html',
  styleUrl: './molen-dialog.component.scss'
})
export class MolenDialogComponent implements OnDestroy{
  public molen?: MolenDataClass;
  public status: "initial" | "uploading" | "success" | "fail" = "initial";
  public file: File | null = null;
  public imagePreview: string | null = null;
  public APIKey: string = "";
  public molenImages: MolenImage[] = [];
  public selectedImage?: MolenImage;

  public imagesAdded: boolean = false;
  get HasImagesLeft(): boolean {
    if (this.molen == undefined || this.molen.addedImages == undefined) return false;
    return this.molen.addedImages.length > 0;
  }

  isExpanded = false;

  deleteImageFunction = this.deleteImage.bind(this);

  constructor(private toasts: Toasts,
    private cdr: ChangeDetectorRef,
    private molenService: MolenService,
    private dialogRef: MatDialogRef<MolenDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { tenBruggeNr: string , molen:MolenData}
  ) { }

  ngOnInit(): void {
    if (!this.data.molen && !this.data.tenBruggeNr) {
      this.onClose();
    }
    if (this.data.molen) {
      this.molen = this.data.molen;
      this.molenImages = this.getAllMolenImages();
      this.selectedImage = this.molenImages[0];
    }
    if (this.data.tenBruggeNr) {
      this.molenService.getMolen(this.data.tenBruggeNr).subscribe({
        next: (molen: MolenData) => {
          this.molen = molen;
          this.molenImages = this.getAllMolenImages();
          this.selectedImage = this.molenImages[0];
        },
        error: (error) => {
          this.toasts.showError(error.error.message);
        }
      })
    }
  }

  ngOnDestroy(): void {
    this.onClose();
  }

  onClose(): void {
    this.dialogRef.close(this.molenImages);
  }

  expandDetails() {
    this.isExpanded = !this.isExpanded
  }

  removeImg(): void {
    this.file = null;
  }

  deleteImage(imgName: string, api_key: string): Observable<any> {
    if (!this.molen || !this.molen.ten_Brugge_Nr) {
      return of();
    }
    return this.molenService.deleteImage(this.molen.ten_Brugge_Nr, imgName, api_key);
  }

  onFileSelected(event: any): void {
    const uploadedFile: File = event.target.files[0];

    if (uploadedFile) {
      this.status = "initial";
      this.file = uploadedFile;
      const reader = new FileReader();
      reader.onload = (e) => {
        this.imagePreview = e.target?.result as string;
      };
      reader.readAsDataURL(this.file);
    }
  }

  onSubmit(): void {
    if (this.file && this.molen) {
      this.status = "uploading";

      const formData = new FormData();
      formData.append('image', this.file, this.file.name);

      var previousMolenImages: MolenImage[] = this.getAllMolenImages();

      this.molenService.uploadImage(this.molen.ten_Brugge_Nr, formData, this.APIKey).subscribe({
          next: (molen: MolenData) => {
            this.molen = molen;
            this.cdr.detectChanges();
          },
          error: (error) => {
            this.status = "fail";
            if (error.status == 401) {
              this.toasts.showError("Er is een verkeerde api key ingevuld!");
            } else {
              this.toasts.showError(error.error.message);
            }
          },
          complete: () => {
            this.removeImg();
            this.molenImages = this.getAllMolenImages();
            this.selectedImage = this.molenImages.find(x => !previousMolenImages.find(y => y.name == x.name));
            this.toasts.showSuccess("Image is saved successfully!");
            this.imagesAdded = true;
          }
        });
      this.APIKey = "";
    }
  }

  getAllMolenImages(): MolenImage[] {
    var AllImages: MolenImage[] = [];
    if (this.molen) {
      if (this.molen.image) {
        AllImages.push(this.molen.image);
      }
      if (this.molen.addedImages && this.molen.addedImages.length > 0) {
        AllImages = AllImages.concat(this.molen.addedImages);
      }
    }
    return AllImages;
  }
}
