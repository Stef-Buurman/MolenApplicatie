import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { SafeUrl } from '@angular/platform-browser';
import { MolenImage } from '../../Class/MolenImage';
import { HttpClient } from '@angular/common/http';
import { DialogReturnStatus } from '../../Enums/DialogReturnStatus';

@Component({
  selector: 'app-image-dialog',
  templateUrl: './image-dialog.component.html',
  styleUrl: './image-dialog.component.css'
})
export class ImageDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public data: {
    selectedImage: MolenImage, onDelete: (name: string) => boolean },
    private dialogRef: MatDialogRef<ImageDialogComponent>,
    private http: HttpClient) { }

  onClose(): void {
    this.dialogRef.close();
  }

  deleteImage(): void {
    if (this.data.onDelete(this.data.selectedImage.name)) {
      this.onClose();
    }
  }
}
