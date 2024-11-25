import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapActiveMolensComponent } from './map-active-molens.component';

describe('MapActiveMolensComponent', () => {
  let component: MapActiveMolensComponent;
  let fixture: ComponentFixture<MapActiveMolensComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [MapActiveMolensComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MapActiveMolensComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
