import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MapRootComponent } from './map-root/map-root.component';
import { OpenMolenDetailsComponent } from './open-molen-details/open-molen-details.component';

const routes: Routes = [
  {
    path: '', component: MapRootComponent, children: [
      { path: ':TenBruggeNumber', component: OpenMolenDetailsComponent } // Child route
    ]
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
