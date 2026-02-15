// src/components/orders/tabs/HistoryTab.tsx
/**
 * ისტორიის ტაბი (Workflow History)
 * აჩვენებს შეკვეთის სტატუსის ცვლილებების ისტორიას
 * თითოეული ჩანაწერი: ვინ, როდის, საიდან სად გადაიყვანა, კომენტარით
 */

//import type { WorkflowEntry } from "./order";

interface HistoryTabProps {
  history: WorkflowEntry[];
}

interface WorkflowEntry {
  fromStatusNameKa?: string;
  toStatusNameKa: string;
  changedByName?: string;
  changedAt: string;
  comments?: string;
}

export default function HistoryTab({ history }: HistoryTabProps) {
  if (history.length === 0) {
    return (
      <div className="text-center py-12 text-muted-foreground border rounded-lg">
        ცვლილებების ისტორია ჯერ არ არსებობს
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {history.map((entry, index) => (
        <div key={index} className="p-4 border rounded-lg bg-white">
          <div className="flex justify-between items-start mb-2">
            <div className="flex items-center gap-2">
              <div className="text-sm font-medium">
                {entry.fromStatusNameKa ? `${entry.fromStatusNameKa} → ` : ""}
                <span className="font-semibold">{entry.toStatusNameKa}</span>
              </div>
            </div>
            <div className="text-xs text-muted-foreground">
              {new Date(entry.changedAt).toLocaleString("ka-GE", {
                dateStyle: "medium",
                timeStyle: "short",
              })}
            </div>
          </div>

          <p className="text-sm text-muted-foreground">
            შეცვალა: {entry.changedByName || "სისტემა"}
          </p>

          {entry.comments && (
            <div className="mt-3 pt-3 border-t">
              <p className="text-sm font-medium mb-1">კომენტარი:</p>
              <p className="text-sm whitespace-pre-wrap">{entry.comments}</p>
            </div>
          )}
        </div>
      ))}
    </div>
  );
}