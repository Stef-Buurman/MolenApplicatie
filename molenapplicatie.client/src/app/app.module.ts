import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialogModule } from '@angular/material/dialog';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { MapComponent } from './map/map.component';
import { MolenDialogComponent } from './molen-dialog/molen-dialog.component';
import { FormsModule } from '@angular/forms';
import { ImageSelectorComponent } from './image-selector/image-selector.component';
import { ImageDialogComponent } from './image-dialog/image-dialog.component';
import { ToastrModule } from 'ngx-toastr';
import { ConfirmationDialogComponent } from './confirmation-dialog/confirmation-dialog.component';
import { ToastComponent } from './toast/toast.component';
import { Toasts } from '../Utils/Toasts';
import { LoaderComponent } from './loader/loader.component';
import { ErrorMessageComponent } from './error-message/error-message.component';
import { DropdownComponent } from './dropdown/dropdown.component';
import { MapRootComponent } from './map-root/map-root.component';
import { ErrorService } from '../Services/ErrorService';
import { OpenMolenDetailsComponent } from './open-molen-details/open-molen-details.component';

@NgModule({
  declarations: [
    AppComponent,
    MapComponent,
    MolenDialogComponent,
    ImageSelectorComponent,
    ImageDialogComponent,
    ConfirmationDialogComponent,
    ToastComponent,
    LoaderComponent,
    ErrorMessageComponent,
    DropdownComponent,
    MapRootComponent,
    OpenMolenDetailsComponent
  ],
  imports: [
    BrowserAnimationsModule,
    BrowserModule,
    HttpClientModule,
    AppRoutingModule,
    MatDialogModule,
    FormsModule,
    ToastrModule.forRoot()
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
