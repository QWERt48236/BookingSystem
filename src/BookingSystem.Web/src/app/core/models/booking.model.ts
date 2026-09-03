export interface BookingRequest {
  slotId: number;
  date: string;
}

export interface BookingResponse {
  id: number;
  slotId: number;
  userId: string;
  date: string;
  createdAt: string;
}
