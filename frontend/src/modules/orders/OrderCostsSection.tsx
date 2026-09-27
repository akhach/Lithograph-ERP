import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import Stack from '@mui/material/Stack'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useEffect, useState } from 'react'
import { fieldMessage, messageOf } from '../auth/apiMessages.ts'
import {
  createCostItem,
  deleteCostItem,
  listCostItems,
  reorderCostItems,
  updateCostItem,
  type CostItem,
  type CostItemInput,
} from './costItemsApi.ts'
import { money } from './ordersApi.ts'

type Draft = {
  category: string
  supplier: string
  expenseDate: string
  description: string
  amount: string
}

const emptyDraft: Draft = {
  category: '',
  supplier: '',
  expenseDate: '',
  description: '',
  amount: '',
}

export function OrderCostsSection({
  orderId,
  canView,
  canEdit,
  readOnlyNote,
  onOrderChanged,
}: {
  orderId: string
  canView: boolean
  canEdit: boolean
  readOnlyNote: string | null
  onOrderChanged: () => Promise<void>
}) {
  const [items, setItems] = useState<CostItem[] | null>(null)
  const [totalCost, setTotalCost] = useState<number | null>(null)
  const [loading, setLoading] = useState(canView)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string | undefined>>({})
  const [draft, setDraft] = useState<Draft>(emptyDraft)
  const [editing, setEditing] = useState<CostItem | null>(null)
  const [adding, setAdding] = useState(false)
  const [pendingDelete, setPendingDelete] = useState<CostItem | null>(null)

  useEffect(() => {
    if (!canView) return

    let cancelled = false
    void (async () => {
      await Promise.resolve()
      if (cancelled) return
      setLoading(true)
      setError(null)
      try {
        const loaded = await listCostItems(orderId)
        if (cancelled) return
        setItems(loaded.items)
        setTotalCost(loaded.totalCost)
      } catch (caught: unknown) {
        if (!cancelled) {
          setItems(null)
          setTotalCost(null)
          setError(messageOf(caught))
        }
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [orderId, canView])

  async function reload() {
    if (!canView) return
    const loaded = await listCostItems(orderId)
    setItems(loaded.items)
    setTotalCost(loaded.totalCost)
  }

  function openAdd() {
    setEditing(null)
    setDraft(emptyDraft)
    setFieldErrors({})
    setError(null)
    setAdding(true)
  }

  function openEdit(item: CostItem) {
    setAdding(false)
    setEditing(item)
    setDraft({
      category: item.category,
      supplier: item.supplier ?? '',
      expenseDate: item.expenseDate ?? '',
      description: item.description ?? '',
      amount: item.amount.toFixed(2),
    })
    setFieldErrors({})
    setError(null)
  }

  function closeForm() {
    setAdding(false)
    setEditing(null)
    setFieldErrors({})
  }

  async function saveForm() {
    setSaving(true)
    setError(null)
    setFieldErrors({})
    try {
      const input = toInput(draft)
      if (editing) {
        await updateCostItem(orderId, editing.id, input)
      } else {
        await createCostItem(orderId, input)
      }
      closeForm()
      await reload()
      await onOrderChanged()
    } catch (caught: unknown) {
      setError(messageOf(caught))
      setFieldErrors({
        category: fieldMessage(caught, 'category'),
        supplier: fieldMessage(caught, 'supplier'),
        expenseDate: fieldMessage(caught, 'expenseDate'),
        description: fieldMessage(caught, 'description'),
        amount: fieldMessage(caught, 'amount'),
      })
    } finally {
      setSaving(false)
    }
  }

  async function confirmDelete() {
    if (!pendingDelete) return
    setSaving(true)
    setError(null)
    try {
      await deleteCostItem(orderId, pendingDelete.id)
      setPendingDelete(null)
      await reload()
      await onOrderChanged()
    } catch (caught: unknown) {
      setError(messageOf(caught))
    } finally {
      setSaving(false)
    }
  }

  async function move(index: number, direction: -1 | 1) {
    if (!items) return
    const target = index + direction
    if (target < 0 || target >= items.length) return
    const ids = items.map((item) => item.id)
    const [moved] = ids.splice(index, 1)
    ids.splice(target, 0, moved)
    setSaving(true)
    setError(null)
    try {
      const loaded = await reorderCostItems(orderId, ids)
      setItems(loaded.items)
      setTotalCost(loaded.totalCost)
    } catch (caught: unknown) {
      setError(messageOf(caught))
    } finally {
      setSaving(false)
    }
  }

  const formOpen = adding || editing !== null

  return (
    <Stack spacing={1}>
      <Typography variant="h6" component="h3">
        Costs
      </Typography>
      {readOnlyNote ? <Alert severity="info">{readOnlyNote}</Alert> : null}
      {error && !formOpen ? <Alert severity="error">{error}</Alert> : null}
      {canView && loading ? <Typography>Loading Costs...</Typography> : null}
      {canView && !loading && items && items.length === 0 ? (
        <Typography>No Cost Items yet.</Typography>
      ) : null}
      {canView && !loading && items && items.length > 0 ? (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Category</TableCell>
              <TableCell>Supplier</TableCell>
              <TableCell>Date</TableCell>
              <TableCell>Description</TableCell>
              <TableCell align="right">Amount</TableCell>
              {canEdit ? <TableCell /> : null}
            </TableRow>
          </TableHead>
          <TableBody>
            {items.map((item, index) => (
              <TableRow key={item.id}>
                <TableCell>{item.category}</TableCell>
                <TableCell>{item.supplier ?? ''}</TableCell>
                <TableCell>{item.expenseDate ?? ''}</TableCell>
                <TableCell>{item.description ?? ''}</TableCell>
                <TableCell align="right">{money(item.amount)}</TableCell>
                {canEdit ? (
                  <TableCell>
                    <Stack direction="row" spacing={1}>
                      <Button size="small" disabled={saving} onClick={() => openEdit(item)}>
                        Edit
                      </Button>
                      <Button
                        size="small"
                        disabled={saving || index === 0}
                        onClick={() => void move(index, -1)}
                      >
                        Up
                      </Button>
                      <Button
                        size="small"
                        disabled={saving || index === items.length - 1}
                        onClick={() => void move(index, 1)}
                      >
                        Down
                      </Button>
                      <Button
                        size="small"
                        color="error"
                        disabled={saving}
                        onClick={() => setPendingDelete(item)}
                      >
                        Delete
                      </Button>
                    </Stack>
                  </TableCell>
                ) : null}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      ) : null}
      {canView && !loading && totalCost !== null ? (
        <Typography>Total cost: {money(totalCost)}</Typography>
      ) : null}
      {canEdit ? (
        <Button variant="outlined" sx={{ alignSelf: 'flex-start' }} onClick={openAdd}>
          Add Cost
        </Button>
      ) : null}

      <Dialog open={formOpen} onClose={closeForm} fullWidth maxWidth="sm">
        <DialogTitle>{editing ? 'Edit Cost Item' : 'Add Cost'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {error ? <Alert severity="error">{error}</Alert> : null}
            <TextField
              label="Category"
              required
              value={draft.category}
              error={Boolean(fieldErrors.category)}
              helperText={fieldErrors.category}
              onChange={(event) => setDraft({ ...draft, category: event.target.value })}
            />
            <TextField
              label="Supplier"
              value={draft.supplier}
              error={Boolean(fieldErrors.supplier)}
              helperText={fieldErrors.supplier}
              onChange={(event) => setDraft({ ...draft, supplier: event.target.value })}
            />
            <TextField
              label="Date"
              type="date"
              value={draft.expenseDate}
              error={Boolean(fieldErrors.expenseDate)}
              helperText={fieldErrors.expenseDate}
              slotProps={{ inputLabel: { shrink: true } }}
              onChange={(event) => setDraft({ ...draft, expenseDate: event.target.value })}
            />
            <TextField
              label="Description"
              value={draft.description}
              error={Boolean(fieldErrors.description)}
              helperText={fieldErrors.description}
              onChange={(event) => setDraft({ ...draft, description: event.target.value })}
            />
            <TextField
              label="Amount"
              required
              value={draft.amount}
              error={Boolean(fieldErrors.amount)}
              helperText={fieldErrors.amount}
              onChange={(event) => setDraft({ ...draft, amount: event.target.value })}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeForm}>Cancel</Button>
          <Button variant="contained" disabled={saving} onClick={() => void saveForm()}>
            Save
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={pendingDelete !== null} onClose={() => setPendingDelete(null)}>
        <DialogTitle>Delete this Cost Item?</DialogTitle>
        <DialogContent>
          <Typography>Order Cost Price will be recalculated.</Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPendingDelete(null)}>Cancel</Button>
          <Button
            variant="contained"
            color="error"
            disabled={saving}
            onClick={() => void confirmDelete()}
          >
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  )
}

function toInput(draft: Draft): CostItemInput {
  return {
    category: draft.category,
    supplier: draft.supplier,
    expenseDate: draft.expenseDate || undefined,
    description: draft.description,
    amount: draft.amount.trim(),
  }
}
