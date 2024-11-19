import { Box, Container, Typography } from "@mui/material";
import { orange, grey } from "@mui/material/colors";
import ShowPosts from "../blogPost/showPosts"; // Display blog posts

const Home = () => {
  return (
    <Container component="main" maxWidth="md" sx={{ paddingTop: 4 }}>
      <Box
        sx={{
          backgroundColor: grey[800],
          padding: 4,
          borderRadius: 2,
          boxShadow: 3,
          textAlign: "center",
        }}
      >
        <Typography
          variant="h3"
          sx={{
            color: orange[500],
            marginBottom: 2,
            fontWeight: "bold",
          }}
        >
          Welcome to Club12 Blog!
        </Typography>
        <Typography
          variant="h6"
          sx={{
            color: grey[300],
            marginBottom: 4,
          }}
        >
          Explore the latest posts from our team.
        </Typography>

        {/* Show blog posts */}
        <ShowPosts />
      </Box>
    </Container>
  );
};

export default Home;
