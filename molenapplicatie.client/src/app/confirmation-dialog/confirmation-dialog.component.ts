import { HttpClient } from '@angular/common/http';
import { Component, Inject, Input } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MolenImage } from '../../Class/MolenImage';
import { ImageDialogComponent } from '../image-dialog/image-dialog.component';
import { ConfirmationDialogData } from '../../Interfaces/ConfirmationDialogData';
import { Toasts } from '../../Utils/Toasts';

@Component({
  selector: 'app-confirmation-dialog',
  templateUrl: './confirmation-dialog.component.html',
  styleUrl: './confirmation-dialog.component.scss'
})
export class ConfirmationDialogComponent {
  public APIKey: string = "";
  constructor(@Inject(MAT_DIALOG_DATA) public data: ConfirmationDialogData,
    private dialogRef: MatDialogRef<ImageDialogComponent>,
    private toasts: Toasts,
    private http: HttpClient) { }

  onClose(isSure: boolean): void {
    if (!isSure) this.toasts.showInfo("Er is niets veranderd!");
    this.dialogRef.close(isSure);
  }

  confirm(): void {
    if (this.data.onConfirm != undefined) {
      if (this.data.api_key_usage) {
        this.data.onConfirm(this.APIKey);
      } else {
        this.data.onConfirm();
      }
    }
    this.onClose(true);
  }
}
