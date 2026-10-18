import { Component, input } from '@angular/core';
@Component({
  selector: 'sd-fastener',
  standalone: true,
  templateUrl: './fastener-art.component.html',
  styleUrl: './fastener-art.component.scss',
})
export class FastenerArt {
  category = input('Șurub');
  threads = [-55, -42, -29, -16, -3, 10, 23, 36, 49, 62, 75, 88];
}
