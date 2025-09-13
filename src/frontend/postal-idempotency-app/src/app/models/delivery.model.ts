export interface Delivery {
  id: string;
  barcode: string;
  employeeId?: string;
  deliveryDate?: Date;
  locationLat?: number;
  locationLng?: number;
  recipientName?: string;
  statusId: number;
  statusNameHe?: string;
  notes?: string;
  createdAt?: Date;
  updatedAt?: Date;
}
