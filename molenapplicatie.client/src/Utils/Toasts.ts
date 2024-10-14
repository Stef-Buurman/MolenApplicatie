import { ToastrService } from 'ngx-toastr';
import { ToastLocation } from '../Enums/ToastLocation';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class Toasts {
  constructor(private toastr: ToastrService) { }

  showSuccess(message: string, title?: string, positionClass: ToastLocation = ToastLocation.BottomRight) {
    this.toastr.success(message, title || 'Success', { positionClass });
  }

  showError(message: string, title?: string, positionClass: ToastLocation = ToastLocation.BottomRight) {
    this.toastr.error(message, title || 'Error', { positionClass });
  }

  showInfo(message: string, title?: string, positionClass: ToastLocation = ToastLocation.BottomRight) {
    this.toastr.info(message, title || 'Information', { positionClass });
  }

  showWarning(message: string, title?: string, positionClass: ToastLocation = ToastLocation.BottomRight) {
    this.toastr.warning(message, title || 'Warning', { positionClass });
  }

  showCustom(message: string, title: string, options: { [key: string]: any }) {
    this.toastr.show(message, title, options);
  }
}
