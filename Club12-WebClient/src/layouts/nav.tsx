import { Toolbar, Typography, Button, Box } from '@mui/material'
import { Outlet, Link as RouterLink } from 'react-router-dom'

const Nav: React.FC = () => {
  return (
    <>
      <Box
        position="static"
        sx={{
          background: 'white',
          color: 'black'
        }}
      >
        <Toolbar>
          <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
            Club 12
          </Typography>
          <Box sx={{ marginRight: 'auto', marginLeft: 2 }}>
            <Button component={RouterLink} to="/home" color="inherit">
              Home
            </Button>
          </Box>
        </Toolbar>
      </Box>
      <Outlet />
    </>
  )
}

export default Nav
