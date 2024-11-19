import { Link } from "react-router-dom";

interface NavMenuProps {
  isAuthenticated: boolean;
}

export const NavMenu: React.FC<NavMenuProps> = ({ isAuthenticated }) => {
  return (
    <div>
      <nav>
        <ul style={{ display: "flex", listStyleType: "none", padding: 0 }}>
          <li style={{ marginRight: "15px" }}>
            <Link to="/" style={{ textDecoration: "none" }}>
              Inicio
            </Link>
          </li>
          <li style={{ marginRight: "15px" }}>
            <Link to="/quienes-somos" style={{ textDecoration: "none" }}>
              Quienes Somos
            </Link>
          </li>
          <li style={{ marginRight: "15px" }}>
            <Link to="/informacion" style={{ textDecoration: "none" }}>
              Información
            </Link>
          </li>
        </ul>
      </nav>
      <div>
        {isAuthenticated ? "Menu for authenticated users" : "Menu for guests"}
      </div>
    </div>
  );
};
