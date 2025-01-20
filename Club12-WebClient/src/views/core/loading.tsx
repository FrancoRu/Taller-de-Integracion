import { CircularProgress, Box } from '@mui/material';
import { grey, orange } from '@mui/material/colors';

const Loading = () => {
  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        height: '100vh',
        backgroundColor: grey[200],
      }}
    >
      <CircularProgress sx={{ color: orange[500] }} />
    </Box>
  );
};

export default Loading;
