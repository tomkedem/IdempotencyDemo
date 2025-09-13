import { Routes } from "@angular/router";

export const routes: Routes = [
  {
    path: "",
    loadComponent: () =>
      import("./components/welcome/welcome.component").then(
        (m) => m.WelcomeComponent
      ),
  },
  {
    path: "create-shipment",
    loadComponent: () =>
      import("./components/create-shipment/create-shipment.component").then(
        (m) => m.CreateShipmentComponent
      ),
  },
  {
    path: "lookup",
    loadComponent: () =>
      import("./components/shipment-lookup/shipment-lookup.component").then(
        (m) => m.ShipmentLookupComponent
      ),
  },
  {
    path: "chaos-control",
    loadComponent: () =>
      import("./components/chaos-control/chaos-control.component").then(
        (m) => m.ChaosControlComponent
      ),
  },
  { path: "**", redirectTo: "" },
];
