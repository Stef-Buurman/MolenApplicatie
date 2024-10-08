import { HttpClient } from '@angular/common/http';
import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MolenDataClass } from '../../Class/MolenDataClass';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
  selector: 'app-molen-dialog',
  templateUrl: './molen-dialog.component.html',
  styleUrl: './molen-dialog.component.css'
})
export class MolenDialogComponent {
  public molen?: MolenDataClass;
  constructor(
    private sanitizer: DomSanitizer,
    private http: HttpClient,
    private dialogRef: MatDialogRef<MolenDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { tenBruggeNr: string }
  ) { }

  ngOnInit(): void {
    this.http.get<MolenDataClass>('/api/molen/' + this.data.tenBruggeNr).subscribe(
      (result) => {
        this.molen = result;
        if (this.molen == undefined) this.onClose();
      },
      (error) => {
        console.error(error);
        this.onClose();
      }
    );
  }

  onClose(): void {
    this.dialogRef.close();
  }

  getImage(data: Uint8Array): any
  {
    let objectURL = 'data:image/png;base64,' + data;
    return this.sanitizer.bypassSecurityTrustUrl(objectURL);
  }

  status: "initial" | "uploading" | "success" | "fail" = "initial";
  file: File | null = null;
  imagePreview: string | null = null;
  onFileSelected(event: any): void {
    const file: File = event.target.files[0];

    if (file) {
      this.status = "initial";
      this.file = file;
      const reader = new FileReader();
      reader.onload = (e) => {
        this.imagePreview = e.target?.result as string;
      };
      reader.readAsDataURL(this.file);
    }
  }

  //onSubmit(): void {
  //  if (this.file && this.file.length > 0) {
  //    const formData = new FormData();

  //    this.file.forEach((file) => {
  //      formData.append('files[]', file, file.name);
  //    });

  //    this.http.post('https://www.primefaces.org/cdn/api/upload.php', formData)
  //      .subscribe(response => {
  //        console.log('Upload success', response);
  //      }, error => {
  //        console.error('Upload failed', error);
  //      });
  //  }
  //}

  onSubmit(): void {
    if (this.file && this.molen) {
      this.status = "uploading"; // Set status to uploading

      const formData = new FormData();
      formData.append('file', this.file, this.file.name);

      // Send the file to your server endpoint
      this.http.post('/api/upload_image/'+this.molen.ten_Brugge_Nr, formData)
        .subscribe({
          next: (response) => {
            console.log('Upload success', response);
            this.status = "success"; // Update status to success
          },
          error: (error) => {
            console.error('Upload failed', error);
            this.status = "fail"; // Update status to fail
          }
        });
    }
  }
}
