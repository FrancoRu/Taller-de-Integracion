import { Grid, Typography, Box } from '@mui/material'
import PropTypes from 'prop-types'

interface ResultsContainerProps {
  text: string
  key: number
}

const ResultsContainer: React.FC<ResultsContainerProps> = ({ text, key }) => {
  return (
        <Grid item key={key} xs={4}>
        <Box sx={{
          height: 100,
          backgroundColor: 'white',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          borderRadius: 3,
          boxShadow: '0 2px 4px rgba(0, 0, 0, 0.6)'
        }}>
          <Typography>
            {text}
          </Typography>
        </Box>
      </Grid>

  )
}

ResultsContainer.propTypes = {
  text: PropTypes.string.isRequired,
  key: PropTypes.number.isRequired
}

export default ResultsContainer
