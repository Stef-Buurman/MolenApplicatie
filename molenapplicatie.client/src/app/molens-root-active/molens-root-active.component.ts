import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-molens-root-active',
  templateUrl: './molens-root-active.component.html',
  styleUrl: './molens-root-active.component.scss'
})
export class MolensRootActiveComponent implements OnInit {
  constructor(private router: Router) { }

  ngOnInit(): void {
    //this.router.navigateByUrl("active");
  }
}
