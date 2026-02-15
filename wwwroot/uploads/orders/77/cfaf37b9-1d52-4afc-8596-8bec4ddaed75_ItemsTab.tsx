// src/components/orders/tabs/ItemsTab.tsx
/**
 * ნივთების ტაბი
 * აჩვენებს შეკვეთაში შეტანილ პოზიციებს ცხრილის სახით
 */

//import type { OrderItem } from "./order";

interface ItemsTabProps {
  items: OrderItem[];
  currency: string;
}

interface OrderItem {
  id: number;
  itemName?: string;
  quantity: number;
  unitPrice?: number;
  totalPrice?: number;
}

export default function ItemsTab({ items, currency }: ItemsTabProps) {
  if (items.length === 0) {
    return (
      <div className="text-center py-12 text-muted-foreground">
        ნივთები არ არის დამატებული
      </div>
    );
  }

  return (
    <div className="border rounded-lg overflow-hidden">
      <table className="w-full text-sm">
        <thead className="bg-muted">
          <tr>
            <th className="text-left p-4 font-medium">დასახელება</th>
            <th className="text-center p-4 font-medium">რაოდენობა</th>
            <th className="text-right p-4 font-medium">ერთ. ფასი</th>
            <th className="text-right p-4 font-medium">ჯამი</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id} className="border-t hover:bg-muted/50 transition-colors">
              <td className="p-4">{item.itemName || "—"}</td>
              <td className="text-center p-4">{item.quantity}</td>
              <td className="text-right p-4">
                {item.unitPrice ? `${item.unitPrice} ${currency}` : "—"}
              </td>
              <td className="text-right p-4 font-medium">
                {item.totalPrice ? `${item.totalPrice} ${currency}` : "—"}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}