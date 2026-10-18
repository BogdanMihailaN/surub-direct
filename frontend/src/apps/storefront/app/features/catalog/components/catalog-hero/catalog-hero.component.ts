import { Component } from '@angular/core';
import { FastenerArt } from '@shared/ui/fastener-art/fastener-art.component';
@Component({
  selector: 'sd-catalog-hero',
  imports: [FastenerArt],
  templateUrl: './catalog-hero.component.html',
  styleUrl: './catalog-hero.component.scss',
})
export class CatalogHeroComponent {}
