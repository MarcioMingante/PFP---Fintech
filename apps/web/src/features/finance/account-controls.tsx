"use client";
import { useActionState, useState } from "react";
import { updateAccount, deleteAccount } from "./actions";

export function AccountControls({ id, name }: { id: string; name: string }) {
  const [editing, setEditing] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const [update, updateAction, updating] = useActionState(updateAccount, { error: "" });
  const [deletion, deleteAction, deleting] = useActionState(deleteAccount, { error: "" });
  return <div className="account-controls">
    <button type="button" onClick={() => setEditing(!editing)}>{editing ? "Cancelar edição" : "Editar"}</button>
    {editing && <form action={updateAction} className="finance-form">
      <input type="hidden" name="id" value={id} />
      <label htmlFor={`rename-${id}`}>Novo nome da conta</label>
      <input id={`rename-${id}`} name="name" defaultValue={name} required maxLength={100} />
      {update.error && <p role="alert">{update.error}</p>}
      <button className="button" disabled={updating}>Salvar alteração</button>
    </form>}
    <button type="button" onClick={() => setConfirming(!confirming)}>{confirming ? "Cancelar exclusão" : "Excluir"}</button>
    {confirming && <form action={deleteAction}>
      <input type="hidden" name="id" value={id} />
      <p>Excluir conta “{name}”? Esta ação não pode ser desfeita. Contas em uso são preservadas.</p>
      <button className="button" disabled={deleting}>Confirmar exclusão</button>
    </form>}
    {deletion.error && <p role="alert">{deletion.error}</p>}
  </div>;
}
