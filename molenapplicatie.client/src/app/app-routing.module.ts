import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { RootComponent } from './root/root.component';
import { OpenMolenDetailsComponent } from './open-molen-details/open-molen-details.component';
import { MapActiveMolensComponent } from './map-active-molens/map-active-molens.component';
import { MolensRootActiveComponent } from './molens-root-active/molens-root-active.component';
import { MapExistingMolensComponent } from './map-existing-molens/map-existing-molens.component';
import { MapDisappearedMolensComponent } from './map-disappeared-molens/map-disappeared-molens.component';
import { MapRemainderMolensComponent } from './map-remainder-molens/map-remainder-molens.component';

const routes: Routes = [
  {
    path: '', component: RootComponent, children: [
      {
        path: '', component: MolensRootActiveComponent, children: [
          { path: '', redirectTo: '/active', pathMatch: 'full' },
          {
            path: 'active', component: MapActiveMolensComponent, children: [
              { path: ':TenBruggeNumber', component: OpenMolenDetailsComponent }
            ]
          },
          {
            path: 'disappeared/:provincie', component: MapDisappearedMolensComponent, children: [
              { path: ':TenBruggeNumber', component: OpenMolenDetailsComponent }
            ]
          },
          {
            path: 'existing', component: MapExistingMolensComponent, children: [
              { path: ':TenBruggeNumber', component: OpenMolenDetailsComponent }
            ]
          },
          {
            path: 'remainder', component: MapRemainderMolensComponent, children: [
              { path: ':TenBruggeNumber', component: OpenMolenDetailsComponent }
            ]
          }
        ]
      }
    ]
  }
];


@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
