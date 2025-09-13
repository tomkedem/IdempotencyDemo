import { Component } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterOutlet, RouterModule, Router } from "@angular/router";
import { MatToolbarModule } from "@angular/material/toolbar";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatCardModule } from "@angular/material/card";

@Component({
  selector: "app-root",
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
  ],
  templateUrl: "./app.component.html",
  styleUrls: ["./app.component.scss"],
})
export class AppComponent {
  title = "Postal Idempotency Demo";

  navigationItems = [
    { path: "/shipments", label: "Shipments", icon: "local_shipping" },
    { path: "/demo", label: "Demo", icon: "play_arrow" },
    { path: "/logs", label: "Logs", icon: "list_alt" },
  ];

  constructor(private router: Router) {}

  hasActiveRoute(): boolean {
    return this.router.url !== "/" && this.router.url !== "";
  }
}
