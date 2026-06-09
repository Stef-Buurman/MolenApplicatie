import { ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ToastType } from '../../Enums/ToastType';

@Component({
  selector: 'app-toast',
  standalone: false,
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.scss',
})
export class ToastComponent implements OnInit {
  ToastType = ToastType;
  @Input() title: string | undefined;
  @Input() message: string = '';
  @Input() type: ToastType | undefined;
  @Input() duration: number = 3000;
  @Output() closeToast = new EventEmitter<void>();

  isVisible = false;
  private timeoutId: any;

  constructor(private cdr: ChangeDetectorRef) {}

  random_error: number = -1;

  ngOnInit() {
    this.random_error = Math.floor(Math.random() * 3);
    setTimeout(() => {
      this.isVisible = true;
      this.cdr.detectChanges();
      this.startTimer();
    }, 250);
  }

  startTimer() {
    this.timeoutId = setTimeout(() => {
      this.close();
    }, this.duration);
  }

  pauseTimer() {
    clearTimeout(this.timeoutId);
  }

  resumeTimer() {
    this.startTimer();
  }

  close() {
    this.isVisible = false;
    this.cdr.detectChanges();
    setTimeout(() => {
      this.closeToast.emit();
    }, 500);
  }
}
