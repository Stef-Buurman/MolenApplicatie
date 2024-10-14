import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MolenDataClass } from '../../Class/MolenDataClass';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { GetSafeUrl } from '../../Utils/GetSafeUrl';
import { MolenImage } from '../../Class/MolenImage';
import { Toasts } from '../../Utils/Toasts';
import { MolenData } from '../../Interfaces/MolenData';

@Component({
  selector: 'app-molen-dialog',
  templateUrl: './molen-dialog.component.html',
  styleUrl: './molen-dialog.component.css'
})
export class MolenDialogComponent {
  public molen?: MolenDataClass;
  public status: "initial" | "uploading" | "success" | "fail" = "initial";
  public file: File | null = null;
  public imagePreview: string | null = null;
  public APIKey: string = "";

  constructor(
    private sanitizer: DomSanitizer,
    private http: HttpClient,
    private toasts: Toasts,
    private dialogRef: MatDialogRef<MolenDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { tenBruggeNr: string }
  ) { }

  ngOnInit(): void {
    this.http.get<MolenDataClass>('/api/molen/' + this.data.tenBruggeNr).subscribe({
      next: (result) => {
        console.log(result);
        this.molen = result;
        if (this.molen == undefined) this.onClose();
      },
      error: (error) => {
        console.error(error);
        this.toasts.showError(error.message, error.status);
        this.onClose();
      }
    });
  }

  onClose(): void {
    this.dialogRef.close();
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
        'API_Key': this.APIKey,
      });

      const formData = new FormData();
      formData.append('image', this.file, this.file.name);

      this.http.post<MolenData>(`/api/upload_image/${this.molen.ten_Brugge_Nr}`, formData, { headers })
        .subscribe({
          next: (molen: MolenData) => {
            this.molen = molen;
          },
          error: (error) => {
            this.status = "fail";
            console.log(error);
            this.toasts.showError(error.message, error.status);
          },
          complete: () => {
            this.removeImg();
            this.toasts.showSuccess("Image is saved successfully!");
          }
        });
    }
  }

  closeDialog(): void {
    this.dialogRef.close();
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
