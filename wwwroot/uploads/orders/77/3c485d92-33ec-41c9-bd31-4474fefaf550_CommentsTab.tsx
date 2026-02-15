// src/components/orders/tabs/CommentsTab.tsx
/**
 * კომენტარების ტაბი
 * აჩვენებს შეკვეთასთან დაკავშირებულ ყველა კომენტარს
 * თითოეულ კომენტარს აქვს რედაქტირებისა და წაშლის ღილაკები
 * ახალი კომენტარის დამატების ფორმა + შიდა/გარე არჩევანი
 */

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import { getAuthHeader } from "@/utils/auth";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import { toast } from "sonner";
import { Loader2, Edit, Trash2 } from "lucide-react";
//import type { OrderComment } from "./types/order";

interface CommentsTabProps {
  orderId: number;
  comments: OrderComment[];
}

interface OrderComment {
  id: number;
  orderId: number;
  userId: number;
  userName: string;
  comment: string;
  isInternal: boolean;
  createdAt: string;
  updatedAt: string;
}

export default function CommentsTab({ orderId, comments }: CommentsTabProps) {
  const queryClient = useQueryClient();

  // ახალი კომენტარის დამატება
  const [newComment, setNewComment] = useState("");
  const [isInternal, setIsInternal] = useState(false);

  // რედაქტირებისთვის
  const [editingCommentId, setEditingCommentId] = useState<number | null>(null);
  const [editCommentText, setEditCommentText] = useState("");

  // Mutations
  const addCommentMutation = useMutation({
    mutationFn: async () => {
      return axios.post(`https://localhost:7048/api/orders/${orderId}/comments`, {
        comment: newComment,
        isInternal,
      }, {
        headers: { Authorization: getAuthHeader() },
      });
    },
    onSuccess: () => {
      toast.success("კომენტარი დაემატა");
      setNewComment("");
      setIsInternal(false);
      queryClient.invalidateQueries({ queryKey: ["order", orderId.toString()] });
    },
    onError: () => toast.error("კომენტარის დამატება ვერ მოხერხდა"),
  });

  const updateCommentMutation = useMutation({
    mutationFn: async ({ commentId, text }: { commentId: number; text: string }) => {
      return axios.put(`https://localhost:7048/api/orders/${orderId}/comments/${commentId}`, {
        comment: text,
      }, {
        headers: { Authorization: getAuthHeader() },
      });
    },
    onSuccess: () => {
      toast.success("კომენტარი განახლდა");
      setEditingCommentId(null);
      setEditCommentText("");
      queryClient.invalidateQueries({ queryKey: ["order", orderId.toString()] });
    },
    onError: () => toast.error("კომენტარის განახლება ვერ მოხერხდა"),
  });

  const deleteCommentMutation = useMutation({
    mutationFn: async (commentId: number) => {
      return axios.delete(`https://localhost:7048/api/orders/${orderId}/comments/${commentId}`, {
        headers: { Authorization: getAuthHeader() },
      });
    },
    onSuccess: () => {
      toast.success("კომენტარი წაიშალა");
      queryClient.invalidateQueries({ queryKey: ["order", orderId.toString()] });
    },
    onError: () => toast.error("კომენტარის წაშლა ვერ მოხერხდა"),
  });

  return (
    <div className="space-y-6">
      {/* კომენტარების სია */}
      {comments.length === 0 ? (
        <div className="text-center py-12 text-muted-foreground border rounded-lg">
          კომენტარები ჯერ არ არის
        </div>
      ) : (
        <div className="space-y-4">
          {comments.map((c) => (
            <div
              key={c.id}
              className={`p-4 border rounded-lg ${
                c.isInternal ? "bg-amber-50 border-amber-200" : "bg-white"
              }`}
            >
              <div className="flex justify-between items-start mb-2">
                <div>
                  <p className="font-medium">{c.userName}</p>
                  <p className="text-xs text-muted-foreground">
                    {new Date(c.createdAt).toLocaleString("ka-GE", {
                      dateStyle: "medium",
                      timeStyle: "short",
                    })}
                  </p>
                </div>
                {c.isInternal && (
                  <Badge variant="secondary" className="text-xs">
                    შიდა
                  </Badge>
                )}
              </div>

              {editingCommentId === c.id ? (
                <div className="space-y-3 mt-2">
                  <Textarea
                    value={editCommentText}
                    onChange={(e) => setEditCommentText(e.target.value)}
                    rows={3}
                    className="resize-none"
                  />
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      onClick={() => updateCommentMutation.mutate({ commentId: c.id, text: editCommentText })}
                      disabled={updateCommentMutation.isPending}
                    >
                      {updateCommentMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                      შენახვა
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => {
                        setEditingCommentId(null);
                        setEditCommentText("");
                      }}
                    >
                      გაუქმება
                    </Button>
                  </div>
                </div>
              ) : (
                <>
                  <p className="text-sm whitespace-pre-wrap">{c.comment}</p>
                  <div className="flex gap-2 mt-3">
                    <Button
                      size="sm"
                      variant="ghost"
                      className="h-8 px-2"
                      onClick={() => {
                        setEditingCommentId(c.id);
                        setEditCommentText(c.comment);
                      }}
                    >
                      <Edit className="h-3.5 w-3.5 mr-1" /> რედაქტირება
                    </Button>
                    <Button
                      size="sm"
                      variant="destructive"
                      className="h-8 px-2"
                      onClick={() => {
                        if (window.confirm("ნამდვილად გსურთ ამ კომენტარის წაშლა?")) {
                          deleteCommentMutation.mutate(c.id);
                        }
                      }}
                    >
                      <Trash2 className="h-3.5 w-3.5" /> წაშლა
                    </Button>
                  </div>
                </>
              )}
            </div>
          ))}
        </div>
      )}

      {/* ახალი კომენტარის დამატება */}
      <div className="pt-6 border-t">
        <h3 className="text-lg font-medium mb-4">ახალი კომენტარის დამატება</h3>
        <div className="space-y-4">
          <Textarea
            placeholder="დაწერეთ თქვენი კომენტარი..."
            value={newComment}
            onChange={(e) => setNewComment(e.target.value)}
            rows={4}
            className="resize-none"
          />

          <div className="flex items-center justify-between">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={isInternal}
                onChange={(e) => setIsInternal(e.target.checked)}
                className="rounded border-gray-300 text-primary focus:ring-primary"
              />
              <span className="text-sm text-muted-foreground">
                შიდა კომენტარი (ხილული მხოლოდ ადმინისტრატორებისთვის)
              </span>
            </label>

            <Button
              onClick={() => addCommentMutation.mutate()}
              disabled={!newComment.trim() || addCommentMutation.isPending}
            >
              {addCommentMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              დამატება
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}