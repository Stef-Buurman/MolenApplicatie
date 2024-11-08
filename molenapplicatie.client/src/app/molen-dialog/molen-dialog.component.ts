import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ChangeDetectorRef, Component, Inject, OnDestroy } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MolenDataClass } from '../../Class/MolenDataClass';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { MolenImage } from '../../Class/MolenImage';
import { Toasts } from '../../Utils/Toasts';
import { MolenData } from '../../Interfaces/MolenData';
import { DialogReturnType } from '../../Interfaces/DialogReturnType';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';
import { MolenDialogReturnType } from '../../Interfaces/MolenDialogReturnType';

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
  public imagesRemoved: boolean = false;
  get HasImagesLeft(): boolean {
    if (this.molen == undefined || this.molen.addedImages == undefined) return false;
    return this.molen.addedImages.length > 0;
  }

  constructor(
    private sanitizer: DomSanitizer,
    private http: HttpClient,
    private toasts: Toasts,
    private cdr: ChangeDetectorRef,
    private dialogRef: MatDialogRef<MolenDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { tenBruggeNr: string }
  ) { }

  ngOnInit(): void {
    this.http.get<MolenDataClass>('/api/molen/' + this.data.tenBruggeNr).subscribe({
      next: (result) => {
        this.molen = result;
        if (this.molen == undefined) this.onClose();
        this.molenImages = this.getAllMolenImages();
        this.selectedImage = this.molenImages[0];
      },
      error: (error) => {
        this.toasts.showError(error.message, error.status);
        this.onClose();
      }
    });
  }

  ngOnDestroy(): void {
    this.onClose();
  }

  onClose(): void {
    this.dialogRef.close({
      MolenImages: this.molenImages
    } as MolenDialogReturnType);
  }

  removeImg(): void {
    this.file = null;
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

      const headers = new HttpHeaders({
        'Authorization': this.APIKey,
      });

      const formData = new FormData();
      formData.append('image', this.file, this.file.name);

      var previousMolenImages: MolenImage[] = this.getAllMolenImages();

      this.http.post<MolenData>(`/api/upload_image/${this.molen.ten_Brugge_Nr}`, formData, { headers })
        .subscribe({
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
