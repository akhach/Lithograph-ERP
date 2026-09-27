import Card from '@mui/material/Card'
import CardActionArea from '@mui/material/CardActionArea'
import CardContent from '@mui/material/CardContent'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { Link } from 'react-router'
import { useAuth } from '../auth/authContext.ts'
import { PermissionCodes } from '../auth/authTypes.ts'

const reports = [
  {
    to: '/reports/orders',
    title: 'Orders',
    text: 'Filter and total Orders, including selling, cost, and profit when you are allowed to see them.',
  },
  {
    to: '/reports/projects',
    title: 'Projects',
    text: 'See Order counts and totals for each Project.',
  },
  {
    to: '/reports/clients',
    title: 'Clients',
    text: 'See Project and Order totals for each Client.',
  },
  {
    to: '/reports/order-types',
    title: 'Order Types',
    text: 'See how Orders are distributed across Order Types, including inactive types.',
  },
]

export function ReportsPage() {
  const auth = useAuth()
  const cards = auth.hasPermission(PermissionCodes.calculatorViewCosts)
    ? [
        ...reports,
        {
          to: '/reports/costs',
          title: 'Costs',
          text: 'Group Cost Items by category, supplier, order, project, client, or order type.',
        },
      ]
    : reports

  return (
    <Stack spacing={2}>
      <Typography variant="h5" component="h2">
        Reports
      </Typography>
      <Typography variant="body2" color="text.secondary">
        Reports read current Orders, Projects, Clients, and Cost Items. They do not store a separate
        copy of those records.
      </Typography>
      <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap' }}>
        {cards.map((card) => (
          <Card key={card.to} variant="outlined" sx={{ width: 280 }}>
            <CardActionArea component={Link} to={card.to}>
              <CardContent>
                <Typography variant="h6" component="h3">
                  {card.title}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {card.text}
                </Typography>
              </CardContent>
            </CardActionArea>
          </Card>
        ))}
      </Stack>
    </Stack>
  )
}
